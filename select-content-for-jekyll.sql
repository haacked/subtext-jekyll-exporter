SELECT FilePath = (
    CASE c.PostType 
    WHEN 2 THEN 'articles/' + c.EntryName + '.aspx.markdown' 
    ELSE SUBSTRING(CONVERT(VARCHAR, DATEADD(HH, blog.TimeZoneOffset, c.DatePublishedUtc), 120), 1, 10) 
        + '-' + c.EntryName + '.aspx.markdown' 
    END),
c.[Text],
Layout = (CASE c.PostType WHEN 2 THEN 'page' ELSE 'post' END),
Title = REPLACE(c.Title, '"', '&quot;'),
[Date] = SUBSTRING(CONVERT(VARCHAR, DATEADD(HH, blog.TimeZoneOffset, c.DatePublishedUtc), 120), 1, 10) + ' ' + 
    (CASE WHEN blog.TimeZoneOffset < 0 
    THEN '-' else '' END + RIGHT('00' + replace(blog.TimeZoneOffset, '-', ''), 2)) + '00',
Categories = '[' + 
 ISNULL(SUBSTRING(
   (SELECT ',' + cat.Title AS [text()]
    FROM subtext_LinkCategories cat 
    INNER JOIN subtext_Links l
    ON l.CategoryID = cat.CategoryID
    WHERE c.ID = l.PostID
    FOR XML PATH ('')), 2, 1000), '') + ']'
FROM subtext_Content c
INNER JOIN subtext_Config blog
ON c.BlogID = blog.BlogID
WHERE blog.BlogID = 0
 AND c.PostConfig & 1 = 1