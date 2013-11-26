SELECT FilePath = (
	CASE c.PostType 
	WHEN 2 THEN 'articles/' + c.EntryName + '.aspx.markdown' 
	ELSE SUBSTRING(CONVERT(VARCHAR, DATEADD(hh, blog.TimeZoneOffset, c.DatePublishedUtc), 120), 1, 10) + '-' + c.EntryName + '.aspx.markdown' 
	END),
Content = 
'---
layout: ' + (CASE c.PostType WHEN 2 THEN 'page' ELSE 'post' END) + '
title: "' + REPLACE(c.Title, '"', '&quot;') + '"
date: ' + SUBSTRING(CONVERT(VARCHAR, DATEADD(hh, blog.TimeZoneOffset, c.DatePublishedUtc), 120), 1, 10) + '
comments: true
categories: [' + 
 ISNULL(SUBSTRING(
   (SELECT ',' + cat.Title AS [text()]
    FROM subtext_LinkCategories cat 
    INNER JOIN subtext_Links l
    ON l.CategoryID = cat.CategoryID
    WHERE c.ID = l.PostID
    FOR XML PATH ('')), 2, 1000), '') + ']
---
' + CAST(c.[Text] as NVARCHAR(MAX))
FROM subtext_Content c
INNER JOIN subtext_Config blog
ON c.BlogID = blog.BlogID
WHERE blog.BlogID = 0
 AND c.PostConfig & 1 = 1
