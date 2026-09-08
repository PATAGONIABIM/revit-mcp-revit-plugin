using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitMCP.Plugin.Extensions;

namespace RevitMCP.Plugin.Handlers
{
    public static class ProjectInfoHandler
    {
        public static object GetProjectInfo(Document doc)
        {
            if (doc == null)
            {
                return new { error = "No active document found." };
            }

            ProjectInfo pi = doc.ProjectInformation;
            if (pi == null)
            {
                return new { error = "ProjectInformation not found in active document." };
            }

            // Standard built-in parameters
            string projectName = pi.GetParameterString(BuiltInParameter.PROJECT_NAME);
            string projectNumber = pi.GetParameterString(BuiltInParameter.PROJECT_NUMBER);
            string clientName = pi.GetParameterString(BuiltInParameter.CLIENT_NAME);
            string author = pi.GetParameterString(BuiltInParameter.PROJECT_AUTHOR);
            string issueDate = pi.GetParameterString(BuiltInParameter.PROJECT_ISSUE_DATE);
            string status = pi.GetParameterString(BuiltInParameter.PROJECT_STATUS);
            string address = pi.GetParameterString(BuiltInParameter.PROJECT_ADDRESS);
            string buildingName = pi.GetParameterString(BuiltInParameter.PROJECT_BUILDING_NAME);
            string orgName = pi.GetParameterString(BuiltInParameter.PROJECT_ORGANIZATION_NAME);
            string orgDesc = pi.GetParameterString(BuiltInParameter.PROJECT_ORGANIZATION_DESCRIPTION);

            // Collect any additional custom/project/shared parameters
            var customParams = new List<object>();
            var standardBips = new HashSet<int>
            {
                (int)BuiltInParameter.PROJECT_NAME,
                (int)BuiltInParameter.PROJECT_NUMBER,
                (int)BuiltInParameter.CLIENT_NAME,
                (int)BuiltInParameter.PROJECT_AUTHOR,
                (int)BuiltInParameter.PROJECT_ISSUE_DATE,
                (int)BuiltInParameter.PROJECT_STATUS,
                (int)BuiltInParameter.PROJECT_ADDRESS,
                (int)BuiltInParameter.PROJECT_BUILDING_NAME,
                (int)BuiltInParameter.PROJECT_ORGANIZATION_NAME,
                (int)BuiltInParameter.PROJECT_ORGANIZATION_DESCRIPTION
            };

            foreach (Parameter p in pi.Parameters)
            {
                if (p.Definition == null) continue;

                // Check if it's already one of the standard built-ins
                var bip = (p.Definition as InternalDefinition)?.BuiltInParameter;
                if (bip.HasValue && standardBips.Contains((int)bip.Value))
                {
                    continue;
                }

                string val = "";
                if (p.HasValue)
                {
                    switch (p.StorageType)
                    {
                        case StorageType.String:
                            val = p.AsString();
                            break;
                        case StorageType.Integer:
                            val = p.AsInteger().ToString();
                            break;
                        case StorageType.Double:
                            val = p.AsDouble().ToString();
                            break;
                        case StorageType.ElementId:
                            val = p.AsElementId().ToString();
                            break;
                    }
                }

                customParams.Add(new
                {
                    name = p.Definition.Name,
                    value = val,
                    type = p.StorageType.ToString(),
                    is_read_only = p.IsReadOnly
                });
            }

            return new
            {
                project_name = projectName,
                project_number = projectNumber,
                client_name = clientName,
                author = author,
                issue_date = issueDate,
                status = status,
                address = address,
                building_name = buildingName,
                organization_name = orgName,
                organization_description = orgDesc,
                custom_parameters = customParams
            };
        }

        public static object SetProjectInfo(Document doc, dynamic args)
        {
            if (doc == null)
            {
                return new { error = "No active document found." };
            }

            ProjectInfo pi = doc.ProjectInformation;
            if (pi == null)
            {
                return new { error = "ProjectInformation not found in active document." };
            }

            var updatedFields = new List<string>();

            if (args != null)
            {
                if (args.project_name != null)
                {
                    string val = (string)args.project_name;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_NAME, val))
                        updatedFields.Add("project_name");
                }

                if (args.project_number != null)
                {
                    string val = (string)args.project_number;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_NUMBER, val))
                        updatedFields.Add("project_number");
                }

                if (args.client_name != null)
                {
                    string val = (string)args.client_name;
                    if (pi.SetParameter(BuiltInParameter.CLIENT_NAME, val))
                        updatedFields.Add("client_name");
                }

                if (args.author != null)
                {
                    string val = (string)args.author;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_AUTHOR, val))
                        updatedFields.Add("author");
                }

                if (args.issue_date != null)
                {
                    string val = (string)args.issue_date;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_ISSUE_DATE, val))
                        updatedFields.Add("issue_date");
                }

                if (args.status != null)
                {
                    string val = (string)args.status;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_STATUS, val))
                        updatedFields.Add("status");
                }

                if (args.address != null)
                {
                    string val = (string)args.address;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_ADDRESS, val))
                        updatedFields.Add("address");
                }

                if (args.building_name != null)
                {
                    string val = (string)args.building_name;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_BUILDING_NAME, val))
                        updatedFields.Add("building_name");
                }

                if (args.organization_name != null)
                {
                    string val = (string)args.organization_name;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_ORGANIZATION_NAME, val))
                        updatedFields.Add("organization_name");
                }

                if (args.organization_description != null)
                {
                    string val = (string)args.organization_description;
                    if (pi.SetParameter(BuiltInParameter.PROJECT_ORGANIZATION_DESCRIPTION, val))
                        updatedFields.Add("organization_description");
                }

                // Custom parameters map (e.g. { "CustomParam": "Value" })
                if (args.custom_parameters != null)
                {
                    foreach (var prop in args.custom_parameters)
                    {
                        string paramName = prop.Name;
                        Parameter p = pi.LookupParameter(paramName);
                        if (p != null && !p.IsReadOnly)
                        {
                            try
                            {
                                if (p.StorageType == StorageType.String)
                                {
                                    p.Set((string)prop.Value);
                                    updatedFields.Add(paramName);
                                }
                                else if (p.StorageType == StorageType.Double)
                                {
                                    p.Set((double)prop.Value);
                                    updatedFields.Add(paramName);
                                }
                                else if (p.StorageType == StorageType.Integer)
                                {
                                    p.Set((int)prop.Value);
                                    updatedFields.Add(paramName);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Failed to set custom parameter {paramName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return new
            {
                success = true,
                updated_fields = updatedFields
            };
        }
    }
}
