using AzureTableStore;
using MPWeb.Common.Attrib;
using System;
using System.Runtime.Serialization;

[Serializable]
[DataContract(Namespace = "PMLogin")]
public class PMLogin : AzureTableObjBase
{
    public const string PartitonConst = "LOGIN";

    [DataMember]
    [AzureTablePartitionKeyAttr]
    public string PartitionKey { get; set; } = PartitonConst;

    [DataMember]
    [AzureTableRowKeyAttr]
    public string Ticks { get; set; }           //belépés időpont ticks sztringesítve

    [DataMember]
    public string Date { get; set; }            //beléps időpont yyyy.MM.dd  (csv exportben ez for megjelenni)

    [DataMember]
    public string DateTime { get; set; }        //beléps időpont  yyyy.MM.dd hh:mm:ss formában (nem exportáljuk, csak hogy hogy meglegyen)

    [DataMember]
    public string OrdNum { get; set; }          //MEgrendelés kódja

    [DataMember]
    public string Name { get; set; }            //Ügyfélnév

    [DataMember]                                
    public string Addr { get; set; }            //Ügyfél címe

    public int  Count { get; set; }             //Belépések száma (az összesítésnek kell, nem mentjük táblába)

}
