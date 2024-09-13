::remove old client files
FOR /F "tokens=7 delims=\" %%G in (..\blacklist.txt) do (
	::force delete, quit mode
	DEL /F /Q ..\%%G
)

::delete assets folder (without confirmation)
RMDIR /S /q assets

::clear/create blacklist folder
break>..\blacklist.txt

::write new files into blacklist.txt from lingit samba share

FOR %%f in (..\..\..\..\masterplast_client_angular\dist\*) do (
	echo %%f >> ..\blacklist.txt
)

xcopy ..\..\..\..\masterplast_client_angular\dist\* ..\*.* /s /c /y

::pause
exit