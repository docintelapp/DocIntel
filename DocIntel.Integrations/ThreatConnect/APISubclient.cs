using System;
using System.Collections.Generic;

namespace DocIntel.Integrations.ThreatConnect
{
    public class APISubclient
    {
        protected static void AddStringFilter(List<string> filters, string filterValue, string filterName)
        {
            if (!string.IsNullOrEmpty(filterValue))
            {
                if (filterValue.StartsWith('=') | filterValue.StartsWith('^'))
                {
                    filters.Add($"{filterName}{filterValue}");
                }
                else
                {
                    filters.Add($"{filterName}={filterValue}");   
                }
            }
        }

        protected static void AddIntFilter(List<string> filters, string filterValue, string filterName)
        {
            if (!string.IsNullOrEmpty(filterValue))
            {
                int t;
                if (filterValue.StartsWith('=') | filterValue.StartsWith('>') | filterValue.StartsWith('<'))
                {
                    if (!int.TryParse(filterValue.Substring(1), out t))
                    {
                        throw new ArgumentException("Date added is not valid");
                    }

                    filters.Add($"{filterName}{filterValue.Substring(0, 1)}{t}");
                }
                else if (!int.TryParse(filterValue, out t))
                {
                    throw new ArgumentException("Date added is not valid");
                }
                else
                {
                    filters.Add($"{filterName}={t}");   
                }
            }
        }

        protected static void AddDoubleFilter(List<string> filters, string filterValue, string filterName)
        {
            if (!string.IsNullOrEmpty(filterValue))
            {
                double t;
                if (filterValue.StartsWith('<') | filterValue.StartsWith('>'))
                {
                    if (!double.TryParse(filterValue.Substring(1), out t))
                    {
                        throw new ArgumentException("Date added is not valid");
                    }

                    filters.Add($"{filterName}{filterValue.Substring(0, 1)}{t}");
                }
                else if (!double.TryParse(filterValue, out t))
                {
                    throw new ArgumentException("Date added is not valid");
                }
                else
                {
                    filters.Add($"{filterName}>{t}");   
                }
            }
        }

        protected static void AddDateTimeFilter(List<string> filters, string filterValue, string filterName)
        {
            if (!string.IsNullOrEmpty(filterValue))
            {
                DateTime t;
                if (filterValue.StartsWith('<') | filterValue.StartsWith('>'))
                {
                    if (!DateTime.TryParse(filterValue.Substring(1), out t))
                    {
                        throw new ArgumentException("Date added is not valid");
                    }

                    filters.Add($"{filterName}{filterValue.Substring(0, 1)}{t:yyyy-MM-ddTHH:mm:ssZ}");
                }
                else if (!DateTime.TryParse(filterValue, out t))
                {
                    throw new ArgumentException("Date added is not valid");
                }
                else
                {
                    filters.Add($"{filterName}>{t:yyyy-MM-ddTHH:mm:ssZ}");                    
                }
            }
        }
    }
}