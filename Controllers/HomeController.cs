using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Text;
using System.Net;
using System.Reflection;
using System.ComponentModel;
using System.Collections;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AspnetInfo.Controllers
{
    public class HomeController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            var sb = new StringBuilder();

            sb.Append("<h1>HttpContext</h1>");
            sb.Append("<table border='1'>");
            sb.AppendKeyValuePair("Application Services", this.HttpContext.ApplicationServices);
            sb.AppendKeyValuePair("Authentication", this.HttpContext.Authentication);
            sb.AppendKeyValuePair("TraceIdentifier", this.HttpContext.TraceIdentifier);
            sb.Append("</table>");

            sb.Append("<h3>HttpContext - Connection</h3>");
            sb.Append(GlobalToStringObject.ToStringObject(this.HttpContext.Connection));

            sb.Append("<h3>HttpContext - Features</h3>");
            sb.Append("<table border='1'>");
            foreach (var f in this.HttpContext.Features)
            {
                sb.AppendKeyValuePair(f.Key.ToString(), f.Value);
            }
            sb.Append("</table>");

            sb.Append("<h3>HttpContext - Items</h3>");
            sb.Append("<table border='1'>");
            foreach (var f in this.HttpContext.Items)
            {
                sb.AppendKeyValuePair(f.Key.ToString(), f.Value);
            }
            sb.Append("</table>");

            sb.Append("<h3>Request</h3>");
            sb.Append(GlobalToStringObject.ToStringObject(Request, 0));

            sb.Append("<h3>Querystring</h3>");
            sb.Append(GlobalToStringObject.ToStringObject(Request.Query, 0));


            sb.Append("<h3>HttpContext - Session</h3>");
            try
            {
                sb.Append(GlobalToStringObject.ToStringObject(this.HttpContext.Session));
            }
            catch (InvalidOperationException ex)
            {
                sb.Append(ex.Message);
            }

            sb.Append("<h3>User</h3>");
            sb.Append(GlobalToStringObject.ToStringObject(this.User, 0));

            sb.Append("<h3>HttpContext - WebSockets</h3>");
            sb.Append(GlobalToStringObject.ToStringObject(this.HttpContext.WebSockets, 0));

            ViewBag.Content = sb.ToString();
            return View();
        }
    }

    static class SBExtensions
    {
        public static void AppendKeyValuePair(this StringBuilder sb, string Key, object Value, bool HtmlEncodeKey = true, bool HtmlEncodeValue = true)
        {
            sb.Append("<tr><td>");
            sb.Append((HtmlEncodeKey ? WebUtility.HtmlEncode(Key) : Key));
            sb.Append("</td><td>");
            sb.Append((HtmlEncodeValue ? WebUtility.HtmlEncode(Value?.ToString()) : Value));
            sb.Append("</td></tr>");
        }
    }

    public class GlobalToStringObject
    {
        public GlobalToStringObject() { }
        public static string ToStringObject(object o, int DrillLevel = 2, HashSet<object> CurrentObjects = null)
        {
            if (DrillLevel < 0)
            {
                return o?.ToString();
            }
            else
            {
                var sb = new StringBuilder();


                if (CurrentObjects == null)
                    CurrentObjects = new HashSet<object>();

                //Get the base type of this object
                var oType = o.GetType();

                if (o is string)
                    sb.Append(o?.ToString());
                else
                {
                    sb.Append("<table border='1'>");

                    //Do the Properties
                    var propertyInfos = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    // sort properties by name
                    Array.Sort(propertyInfos, delegate (PropertyInfo propertyInfo1, PropertyInfo propertyInfo2) { return propertyInfo1.Name.CompareTo(propertyInfo2.Name); });

                    // write property names
                    foreach (var propertyInfo in propertyInfos)
                    {
                        sb.Append("<tr valign='top'><td>");
                        sb.Append(propertyInfo.Name);
                        sb.Append("</td><td>");
                        try
                        {
                            var obj = propertyInfo.GetValue(o, null);

                            //process the property itself (recursive)
                            CheckAndProcessType(obj, sb, DrillLevel, CurrentObjects);
                        }
                        catch (Exception ex)
                        {
                            sb.Append(ex.Message);
                        }
                        sb.Append("</td></tr>");
                    }
                    sb.Append("</table>");
                }

                return sb.ToString();
            }
        }

        public static void CheckAndProcessType(object obj, StringBuilder sb, int DrillLevel, HashSet<object> CurrentObjects, bool IgnoreSelf = false)
        {
            if (DrillLevel < 0)
            {
                sb.Append(obj?.ToString());
            }
            else
            {
                bool isEnumerable = false;
                bool isDictionary = false;
                bool isPrimitive = false;
                bool isNull = false;
                #region Check Type
                if (obj == null)
                {
                    //null object can ignore
                    isNull = true;
                }
                else if (obj.GetType() == typeof(string))
                {
                    //string actually implements ienumerable, so need this check to make sure it does not cross to the next check
                    isPrimitive = true;
                }
                else if (obj.GetType().IsPrimitive)
                {
                    //check if is primitive type
                    isPrimitive = true;
                }
                else
                {
                    //Check if the type is a dictionary or ienumerable
                    foreach (var T in obj.GetType().GetInterfaces())
                    {
                        if (T.UnderlyingSystemType == typeof(IDictionary))
                        {
                            isDictionary = true;
                            break;
                        }
                        else if (T.UnderlyingSystemType == typeof(IEnumerable))
                        {
                            isEnumerable = true;
                        }
                    }
                }
                #endregion

                if (isNull)
                {
                    sb.Append("&nbsp;");
                }
                else if (isDictionary)
                {
                    var IC = (IDictionary)obj;
                    sb.Append("<table border='1'>");
                    foreach (var key in IC.Keys)
                    {
                        sb.Append("<tr valign='top'><td>");
                        //Recursively get the Key to string value
                        sb.Append(WebUtility.HtmlEncode(ToStringObject(key, DrillLevel - 1, CurrentObjects)));
                        sb.Append("</td><td>");
                        //Recursively get the Value to string value
                        sb.Append(ToStringObject(IC[key], DrillLevel - 1, CurrentObjects));
                        sb.Append("</tr>");
                    }
                    sb.Append("</table>");
                }
                else if (isEnumerable)
                {
                    int i = 1;
                    foreach (var oo in (IEnumerable)obj)
                    {
                        sb.Append("<li> #" + i + "</li>");

                        //Recursively get the Item to string value
                        sb.Append("<li>" + ToStringObject(oo, DrillLevel - 1) + "</li>");
                        i++;
                    }
                }
                else
                {
                    if (isPrimitive)
                    {
                        sb.Append(WebUtility.HtmlEncode(obj?.ToString()));
                    }
                    else
                    {
                        if (false && CurrentObjects.Contains(obj))
                        {
                            sb.Append("~");
                        }
                        else
                        {
                            CurrentObjects.Add(obj);
                            sb.Append((IgnoreSelf ? obj?.ToString() : ToStringObject(obj, DrillLevel - 1, CurrentObjects)));
                        }
                    }
                }
            }
        }
        public override string ToString()
        {
            return ToStringObject(this);
        }
    }
}
