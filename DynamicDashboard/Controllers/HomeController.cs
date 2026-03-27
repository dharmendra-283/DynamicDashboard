using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DynamicDashboard.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            string query = @"DECLARE @cols NVARCHAR(MAX), @ColumnForSum AS NVARCHAR(MAX), @RowForSum AS NVARCHAR(MAX), @query NVARCHAR(MAX)
                            ;WITH MonthData AS (
                                SELECT DISTINCT 
		                            LEFT(DATENAME(MONTH, BirthDate), 3) AS MonthName,
                                    CASE 
                                        WHEN MONTH(BirthDate) >= 4 THEN MONTH(BirthDate) - 3
                                        ELSE MONTH(BirthDate) + 9
                                    END AS MonthNumber
                                FROM Employees
                                --WHERE DATENAME(MONTH, BirthDate) IN ('May','August')
                            )

                            SELECT @cols = STRING_AGG(QUOTENAME(MonthName), ',') 
                                           WITHIN GROUP (ORDER BY MonthNumber)
                            FROM MonthData

                            --Row Total
                            SELECT @ColumnForSum = REPLACE(@cols,',','+')
                            SELECT @ColumnForSum = REPLACE(@ColumnForSum,'[','ISNULL([')
                            SELECT @ColumnForSum = REPLACE(@ColumnForSum,']','],0)')

                            --Column Total
                            SELECT @RowForSum = REPLACE(@cols,',',',')
                            SELECT @RowForSum = REPLACE(@RowForSum,'[','SUM(ISNULL([')
                            SELECT @RowForSum = REPLACE(@RowForSum,']','],0))')

                            SET @query = ';WITH cte
                            AS (
	                            SELECT * FROM
                                (
		                            SELECT Country, City, ' + @cols + ', SUM('+@ColumnForSum+') Total
		                            FROM (
			                            SELECT Country, City,
				                            LEFT(DATENAME(MONTH, BirthDate), 3) AS MonthName,
				                            CONVERT(INT, Extension) Extension
			                            FROM Employees
			                            --WHERE DATENAME(MONTH, BirthDate) IN (''May'',''August'')
		                            ) src
		                            PIVOT (
			                            SUM(Extension)
			                            FOR MonthName IN (' + @cols + ')
		                            ) pvt GROUP BY Country, City, '+ @cols +'
	                            )r
                            )
                            SELECT Country, City, '+ @cols +', Total FROM cte
                            ORDER BY City
                            --UNION ALL
                            --SELECT '''', '''', '+ @RowForSum +', SUM(Total) FROM cte
                            '

                            EXEC sp_executesql @query";
            string constr = ConfigurationManager.ConnectionStrings["constr"].ConnectionString;
            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                {
                    using (DataTable dt = new DataTable())
                    {
                        sda.Fill(dt);

                        DataRow totalRow = dt.NewRow();
                        int i = 0;
                        foreach (DataColumn dc in dt.Columns)
                        {
                            if (i > 1)
                            {

                                totalRow[dc.ColumnName] = dt.Compute("Sum(" + dc.ColumnName + ")", string.Empty);
                            }
                            else
                            {
                                totalRow[dc.ColumnName] = string.Empty;
                            }
                            i++;
                        }
                        dt.Rows.Add(totalRow);


                        return View(dt);
                    }
                }
            }
        }
    }
}