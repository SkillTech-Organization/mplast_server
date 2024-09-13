using System;


namespace MPWeb.Common.Attrib
{
    /*Rekord egyedi kulcs. Ha a tárolt adatszerkezet nem hierarchius, akkor a PartitionKeyt- kell használni a rekordazonosításra,
     mert az gyorsabb:
     http://blog.maartenballiauw.be/post/2012/10/08/What-PartitionKey-and-RowKey-are-for-in-Windows-Azure-Table-Storage.aspx
    */
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AzureTableRowKeyAttr : Attribute
    {
    }
}
