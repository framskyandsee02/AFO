using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
[System.Serializable] public class ComboCell {
    // Internal effect recipe only. Players never choose an ordering.
    public BlobProperty primary, secondary;
    public string id, nameTh, effectTh, useTh;
    public bool implemented;
}
[CreateAssetMenu(menuName="OOPS Planet/Combo Table",fileName="ComboTable_")]
public class ComboTable : ScriptableObject {
    public static readonly BlobProperty[] Powers={BlobProperty.Magnet,BlobProperty.Pressure,BlobProperty.Bounce,BlobProperty.Water,BlobProperty.Electric,BlobProperty.Heat};
    public List<ComboCell> cells=new List<ComboCell>();
    public ComboCell Get(BlobProperty a,BlobProperty b) {
        if(a==BlobProperty.None || b==BlobProperty.None || a==b) return null;
        foreach(var c in cells) if(c!=null && Id(c.primary,c.secondary)==Id(a,b)) return c;
        return null;
    }
    public static string Id(BlobProperty a,BlobProperty b) {
        if((int)a>(int)b) { var swap=a; a=b; b=swap; }
        return a.ToString().ToLowerInvariant()+"+"+b.ToString().ToLowerInvariant();
    }
    public static string NormalizeId(string id) {
        if(string.IsNullOrEmpty(id)) return "";
        var parts=id.Split('+'); BlobProperty a,b;
        return parts.Length==2 && System.Enum.TryParse(parts[0],true,out a) && System.Enum.TryParse(parts[1],true,out b)?Id(a,b):id;
    }
}
public static class ComboTableDefaults {
    const BlobProperty M=BlobProperty.Magnet,P=BlobProperty.Pressure,B=BlobProperty.Bounce,W=BlobProperty.Water,E=BlobProperty.Electric,H=BlobProperty.Heat;
    static ComboCell C(BlobProperty a,BlobProperty b,string name,string effect,string use) => new ComboCell{primary=a,secondary=b,id=ComboTable.Id(a,b),nameTh=name,effectTh=effect,useTh=use,implemented=true};
    public static List<ComboCell> Create() => new List<ComboCell>{
        C(M,P,"ปืนแม่เหล็ก","ดูดโลหะแล้วปล่อยยิง","G ค้างดูดโลหะ ปล่อยเพื่อยิง หรือถือโลหะด้วย F แล้วกด G"),
        C(M,B,"สลิงแม่เหล็ก","ดึงตัวเองเข้าหาจุดโลหะ","เล็งจุดโลหะแล้วกด G เพื่อข้ามช่องหรือขึ้นที่สูง"),
        C(W,M,"สายน้ำแม่เหล็ก","ฉีดน้ำลากโลหะเข้าหาตัว","กด G ค้าง ลากแพหรือเศษโลหะ"),
        C(E,M,"แม่เหล็กไฟฟ้า","ชาร์จแม่เหล็กให้ยกของหนักถึง 40 กก.","กด G ชาร์จ แล้วเลือกแม่เหล็กด้วย Q และกด F ค้างเพื่อยก"),
        C(H,M,"ความร้อนเหนี่ยวนำ","ทำโลหะที่เล็งให้ร้อนจากระยะไกล","กด G ค้างเพื่อละลายน้ำแข็งบนโลหะ"),
        C(P,B,"ลูกลมกระดอน","ยิงลูกลมสะท้อนกำแพง","กด G ยิงสวิตช์หลังมุม"),
        C(P,W,"น้ำแรงสูง","ฉีดน้ำไกลและผลักของ","กด G ยิงน้ำแรงสูง ดับไฟ ล้างโคลน หรือผลักของ"),
        C(E,P,"สายฟ้าแรงสูง","ยิงไฟฟ้าไกลและกระจายได้สามเป้า","เล็งแล้วกด G จ่ายไฟเครื่องระยะไกลหรือหลายเครื่อง"),
        C(H,P,"พ่นไฟ","พ่นไฟระยะไกลเป็นทาง","กด G ค้างเพื่อเผาฟางทั้งแนว"),
        C(W,B,"ลูกน้ำเด้ง","โยนลูกน้ำกระดอน","กด G รดพืชบนที่สูงหรือหลังกำแพง"),
        C(E,B,"สายฟ้าชิ่ง","ไฟฟ้าชิ่งต่อได้สี่เป้า","เล็งเป้าแรกแล้วกด G เปิดสวิตช์ตามลำดับหรือแผ่นไฟฟ้า"),
        C(H,B,"ลูกไฟเด้ง","โยนลูกไฟกระดอน","กด G จุดเตาหรือเผาน้ำแข็งหลังกำแพง"),
        C(W,E,"สายน้ำไฟฟ้า","ฉีดน้ำพร้อมส่งกระแสไฟ","กด G ค้างเพื่อจ่ายไฟผ่านน้ำ"),
        C(W,H,"ไอน้ำ","พ่นน้ำร้อนสร้างเมฆไอ","กด G ค้างเพื่อยกตัวหรือป้อนเครื่องไอน้ำ"),
        C(H,E,"เชื่อมไฟฟ้า","ให้ความร้อนพร้อมกระแสไฟ","กด G ค้าง ซ่อมรางโลหะหรือละลายฟิวส์")
    };
}
}
