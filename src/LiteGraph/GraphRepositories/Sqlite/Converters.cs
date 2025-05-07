namespace LiteGraph.GraphRepositories.Sqlite
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Serialization;

    internal static class Converters
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string GetDataRowStringValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    return row[column].ToString();
                }
            }
            return null;
        }

        internal static object GetDataRowJsonValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    return Serializer.DeserializeJson<object>(row[column].ToString());
                }
            }
            return null;
        }

        internal static int GetDataRowIntValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    if (Int32.TryParse(row[column].ToString(), out int val))
                        return val;
                }
            }
            return 0;
        }

        internal static int? GetDataRowNullableIntValue(DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    if (Int32.TryParse(row[column].ToString(), out int val))
                        return val;
                }
            }
            return null;
        }

        internal static bool IsList(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        internal static List<object> ObjectToList(object obj)
        {
            if (obj == null) return null;
            List<object> ret = new List<object>();
            var enumerator = ((IEnumerable)obj).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ret.Add(enumerator.Current);
            }
            return ret;
        }

        internal static string EnumerationOrderToClause(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CostAscending:
                    return "cost ASC";
                case EnumerationOrderEnum.CostDescending:
                    return "cost DESC";
                case EnumerationOrderEnum.CreatedAscending:
                    return "createdutc ASC";
                case EnumerationOrderEnum.CreatedDescending:
                    return "createdutc DESC";
                case EnumerationOrderEnum.GuidAscending:
                    return "id ASC";
                case EnumerationOrderEnum.GuidDescending:
                    return "id DESC";
                case EnumerationOrderEnum.NameAscending:
                    return "name ASC";
                case EnumerationOrderEnum.NameDescending:
                    return "name DESC";
                default:
                    throw new ArgumentException("Unsupported enumeration order '" + order.ToString() + "'.");
            }
        }

        internal static string ExpressionToWhereClause(string table, Expr expr)
        {
            if (expr == null) return null;
            if (expr.Left == null) return null;

            string clause = "(";

            if (expr.Left is Expr)
            {
                clause += ExpressionToWhereClause(table, (Expr)expr.Left) + " ";
            }
            else
            {
                if (!(expr.Left is string))
                {
                    throw new ArgumentException("Left term must be of type Expression or String");
                }

                clause += "json_extract(" + table + ".data, '$." + Sanitizer.Sanitize(expr.Left.ToString()) + "') ";
            }

            switch (expr.Operator)
            {
                #region Process-By-Operators

                case OperatorEnum.And:
                    #region And

                    if (expr.Right == null) return null;
                    clause += "AND ";

                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.Or:
                    #region Or

                    if (expr.Right == null) return null;
                    clause += "OR ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.Equals:
                    #region Equals

                    if (expr.Right == null) return null;
                    clause += "= ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.NotEquals:
                    #region NotEquals

                    if (expr.Right == null) return null;
                    clause += "<> ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.In:
                    #region In

                    if (expr.Right == null) return null;
                    int inAdded = 0;
                    if (!IsList(expr.Right)) return null;
                    List<object> inTempList = ObjectToList(expr.Right);
                    clause += "IN (";
                    foreach (object currObj in inTempList)
                    {
                        if (currObj == null) continue;
                        if (inAdded > 0) clause += ",";
                        if (currObj is DateTime || currObj is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(currObj).ToString(TimestampFormat) + "'";
                        }
                        else if (currObj is int || currObj is long || currObj is decimal)
                        {
                            clause += currObj.ToString();
                        }
                        else if (currObj is bool)
                        {
                            clause += ((bool)currObj) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(currObj.ToString()) + "'";
                        }
                        inAdded++;
                    }
                    clause += ")";
                    break;

                #endregion

                case OperatorEnum.NotIn:
                    #region NotIn

                    if (expr.Right == null) return null;
                    int notInAdded = 0;
                    if (!IsList(expr.Right)) return null;
                    List<object> notInTempList = ObjectToList(expr.Right);
                    clause += "NOT IN (";
                    foreach (object currObj in notInTempList)
                    {
                        if (currObj == null) continue;
                        if (notInAdded > 0) clause += ",";
                        if (currObj is DateTime || currObj is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(currObj).ToString(TimestampFormat) + "'";
                        }
                        else if (currObj is int || currObj is long || currObj is decimal)
                        {
                            clause += currObj.ToString();
                        }
                        else if (currObj is bool)
                        {
                            clause += ((bool)currObj) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(currObj.ToString()) + "'";
                        }
                        notInAdded++;
                    }
                    clause += ")";
                    break;

                #endregion

                case OperatorEnum.Contains:
                    #region Contains

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.ContainsNot:
                    #region ContainsNot

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "NOT LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.StartsWith:
                    #region StartsWith

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "LIKE '" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.StartsWithNot:
                    #region StartsWithNot

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "NOT LIKE '" + Sanitizer.Sanitize(expr.Right.ToString()) + "%'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.EndsWith:
                    #region EndsWith

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.EndsWithNot:
                    #region EndsWith

                    if (expr.Right == null) return null;
                    if (expr.Right is string)
                    {
                        clause += "NOT LIKE '%" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                    }
                    else
                    {
                        return null;
                    }
                    break;

                #endregion

                case OperatorEnum.GreaterThan:
                    #region GreaterThan

                    if (expr.Right == null) return null;
                    clause += "> ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.GreaterThanOrEqualTo:
                    #region GreaterThanOrEqualTo

                    if (expr.Right == null) return null;
                    clause += ">= ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.LessThan:
                    #region LessThan

                    if (expr.Right == null) return null;
                    clause += "< ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.LessThanOrEqualTo:
                    #region LessThanOrEqualTo

                    if (expr.Right == null) return null;
                    clause += "<= ";
                    if (expr.Right is Expr)
                    {
                        clause += ExpressionToWhereClause(table, (Expr)expr.Right);
                    }
                    else
                    {
                        if (expr.Right is DateTime || expr.Right is DateTime?)
                        {
                            clause += "'" + Convert.ToDateTime(expr.Right).ToString(TimestampFormat) + "'";
                        }
                        else if (expr.Right is int || expr.Right is long || expr.Right is decimal)
                        {
                            clause += expr.Right.ToString();
                        }
                        else if (expr.Right is bool)
                        {
                            clause += ((bool)expr.Right) ? "true" : "false";
                        }
                        else
                        {
                            clause += "'" + Sanitizer.Sanitize(expr.Right.ToString()) + "'";
                        }
                    }
                    break;

                #endregion

                case OperatorEnum.IsNull:
                    #region IsNull

                    clause += "IS NULL";
                    break;

                #endregion

                case OperatorEnum.IsNotNull:
                    #region IsNotNull

                    clause += "IS NOT NULL";
                    break;

                    #endregion

                    #endregion
            }

            clause += ") ";

            return clause;
        }

        internal static TenantMetadata TenantFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new TenantMetadata
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                Name = GetDataRowStringValue(row, "name"),
                Active = (Convert.ToInt32(GetDataRowStringValue(row, "active")) > 0 ? true : false),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static UserMaster UserFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new UserMaster
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                FirstName = GetDataRowStringValue(row, "firstname"),
                LastName = GetDataRowStringValue(row, "lastname"),
                Email = GetDataRowStringValue(row, "email"),
                Password = GetDataRowStringValue(row, "password"),
                Active = (Convert.ToInt32(GetDataRowStringValue(row, "active")) > 0 ? true : false),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static Credential CredentialFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Credential
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                UserGUID = Guid.Parse(row["userguid"].ToString()),
                Name = GetDataRowStringValue(row, "name"),
                BearerToken = GetDataRowStringValue(row, "bearertoken"),
                Active = (Convert.ToInt32(GetDataRowStringValue(row, "active")) > 0 ? true : false),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static TagMetadata TagFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new TagMetadata
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                GraphGUID = Guid.Parse(row["graphguid"].ToString()),
                NodeGUID = (!String.IsNullOrEmpty(GetDataRowStringValue(row, "nodeguid")) ? Guid.Parse(row["nodeguid"].ToString()) : null),
                EdgeGUID = (!String.IsNullOrEmpty(GetDataRowStringValue(row, "edgeguid")) ? Guid.Parse(row["edgeguid"].ToString()) : null),
                Key = GetDataRowStringValue(row, "tagkey"),
                Value = GetDataRowStringValue(row, "tagvalue"),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static List<TagMetadata> TagsFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<TagMetadata> ret = new List<TagMetadata>();

            foreach (DataRow row in table.Rows)
                ret.Add(TagFromDataRow(row));

            return ret;
        }

        internal static LabelMetadata LabelFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new LabelMetadata
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                GraphGUID = Guid.Parse(row["graphguid"].ToString()),
                NodeGUID = (!String.IsNullOrEmpty(GetDataRowStringValue(row, "nodeguid")) ? Guid.Parse(row["nodeguid"].ToString()) : null),
                EdgeGUID = (!String.IsNullOrEmpty(GetDataRowStringValue(row, "edgeguid")) ? Guid.Parse(row["edgeguid"].ToString()) : null),
                Label = GetDataRowStringValue(row, "label"),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static List<LabelMetadata> LabelsFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<LabelMetadata> ret = new List<LabelMetadata>();

            foreach (DataRow row in table.Rows)
                ret.Add(LabelFromDataRow(row));

            return ret;
        }

        internal static VectorMetadata VectorFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new VectorMetadata
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                GraphGUID = Guid.Parse(row["graphguid"].ToString()),
                NodeGUID = (!String.IsNullOrEmpty(GetDataRowStringValue(row, "nodeguid")) ? Guid.Parse(row["nodeguid"].ToString()) : null),
                EdgeGUID = (!String.IsNullOrEmpty(GetDataRowStringValue(row, "edgeguid")) ? Guid.Parse(row["edgeguid"].ToString()) : null),
                Model = GetDataRowStringValue(row, "model"),
                Dimensionality = GetDataRowIntValue(row, "dimensionality"),
                Content = GetDataRowStringValue(row, "content"),
                Vectors = Serializer.DeserializeJson<List<float>>(GetDataRowStringValue(row, "embeddings")),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static List<VectorMetadata> VectorsFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<VectorMetadata> ret = new List<VectorMetadata>();

            foreach (DataRow row in table.Rows)
                ret.Add(VectorFromDataRow(row));

            return ret;
        }

        internal static Graph GraphFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Graph
            {
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                GUID = Guid.Parse(row["guid"].ToString()),
                Name = GetDataRowStringValue(row, "name"),
                Data = GetDataRowJsonValue(row, "data"),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static Node NodeFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Node
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                GraphGUID = Guid.Parse(row["graphguid"].ToString()),
                Name = GetDataRowStringValue(row, "name"),
                EdgesIn = GetDataRowNullableIntValue(row, "edges_in"),
                EdgesOut = GetDataRowNullableIntValue(row, "edges_out"),
                Data = GetDataRowJsonValue(row, "data"),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }

        internal static List<Node> NodesFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<Node> ret = new List<Node>();

            foreach (DataRow row in table.Rows)
                ret.Add(NodeFromDataRow(row));

            return ret;
        }

        internal static List<Edge> EdgesFromDataTable(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<Edge> ret = new List<Edge>();

            foreach (DataRow row in table.Rows)
                ret.Add(EdgeFromDataRow(row));

            return ret;
        }

        internal static Edge EdgeFromDataRow(DataRow row)
        {
            if (row == null) return null;

            return new Edge
            {
                GUID = Guid.Parse(row["guid"].ToString()),
                TenantGUID = Guid.Parse(row["tenantguid"].ToString()),
                GraphGUID = Guid.Parse(row["graphguid"].ToString()),
                Name = GetDataRowStringValue(row, "name"),
                From = Guid.Parse(row["fromguid"].ToString()),
                To = Guid.Parse(row["toguid"].ToString()),
                Cost = Convert.ToInt32(row["cost"].ToString()),
                Data = GetDataRowJsonValue(row, "data"),
                CreatedUtc = DateTime.Parse(row["createdutc"].ToString()),
                LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString())
            };
        }
    }
}
