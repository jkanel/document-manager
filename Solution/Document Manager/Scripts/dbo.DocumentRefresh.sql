USE [DocumentManager]
GO

-- exec dbo.DocumentRefresh
GO

CREATE PROCEDURE DocumentRefresh AS
BEGIN

INSERT INTO [dbo].[Document]
           ([DocumentHash]
           ,[OriginalFileName]
           ,[OriginalFolderPath]
           ,[TargetFileName]
		   ,[FileSize]
		   ,[FileType]
		   ,[FileCreatedTimestamp]
		   ,[FileModifiedTimestamp]
           ,[IgnoreFlag]
           ,[ModifyTimestamp])

SELECT
  x.DocumentHash
, x.FileName
, x.FolderPath
, x.FileName
, x.FileSize
, x.FileType
, x.FileCreatedTimestamp
, x.FileModifiedTimestamp
, 0
, CURRENT_TIMESTAMP
FROM 
(
	SELECT
	  df.DocumentHash
	, df.FileName
	, df.FolderPath
	, df.FileSize
	, df.FileType
	, df.FileCreatedTimestamp
	, df.FileModifiedTimestamp
	, ROW_NUMBER() OVER (PARTITION BY df.DocumentHash ORDER BY df.ModifyTimestamp) AS FilterIndex
	FROM
	DocumentFile df
	WHERE
	df.DocumentHash != 'ERROR'
	AND NOT EXISTS (
		SELECT 1 FROM Document d WHERE d.DocumentHash = df.DocumentHash
	)
) x
WHERE
x.FilterIndex = 1
;

UPDATE d SET
d.DocumentHash = df.DocumentHash
FROM
dbo.Document d
INNER JOIN dbo.DocumentFile df ON
	d.OriginalFolderPath = df.FolderPath 
	AND d.OriginalFileName = df.FileName
WHERE
d.DocumentHash != df.DocumentHash
;

END
;
