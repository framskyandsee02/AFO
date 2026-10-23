using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// v2 combination table (Docs/DESIGN_V2_TH.md §4): ORDERED pairs, primary = verb (F), secondary = element (G).
// 6 powers -> 30 cells; Magnet+Pressure and Pressure+Magnet are different cells. Holding the same power twice is impossible.
[System.Serializable]
public class ComboCell {
    public BlobProperty primary, secondary;
    public string id;              // stable key ("magnet+pressure"), used by the power journal / saves
    public string nameTh, effectTh, useTh;
    public bool implemented;       // runtime effect exists (stage C); the table itself is stage B
}
[CreateAssetMenu(menuName="OOPS Planet/Combo Table (v2)",fileName="ComboTable_")]
public class ComboTable : ScriptableObject {
    public static readonly BlobProperty[] Powers={BlobProperty.Magnet,BlobProperty.Pressure,BlobProperty.Bounce,BlobProperty.Water,BlobProperty.Electric,BlobProperty.Heat};
    public List<ComboCell> cells=new List<ComboCell>();
    // Ordered lookup; null for None, same-power or unknown pairs.
    public ComboCell Get(BlobProperty primary,BlobProperty secondary) {
        if(primary==BlobProperty.None || secondary==BlobProperty.None || primary==secondary) return null;
        foreach(var c in cells) if(c!=null && c.primary==primary && c.secondary==secondary) return c;
        return null;
    }
    public static string Id(BlobProperty a,BlobProperty b) => a.ToString().ToLowerInvariant()+"+"+b.ToString().ToLowerInvariant();
}
// Default content of the table, copied from the approved design doc. Used to create the asset once; edit the asset afterwards.
public static class ComboTableDefaults {
    static ComboCell C(BlobProperty a,BlobProperty b,string name,string effect,string use) =>
        new ComboCell{primary=a,secondary=b,id=ComboTable.Id(a,b),nameTh=name,effectTh=effect,useTh=use,
            implemented=(a==BlobProperty.Magnet&&b==BlobProperty.Pressure)||(a==BlobProperty.Bounce&&b==BlobProperty.Pressure)};
    const BlobProperty M=BlobProperty.Magnet,P=BlobProperty.Pressure,B=BlobProperty.Bounce,W=BlobProperty.Water,E=BlobProperty.Electric,H=BlobProperty.Heat;
    public static List<ComboCell> Create() => new List<ComboCell>{
        C(M,P,"ปืนแม่เหล็ก","ยิงโลหะที่ถืออยู่ออกไปแรง","กดปุ่มไกล ๆ, ทุบฝาหรือสลักที่เปราะ"),
        C(M,B,"สลิงแม่เหล็ก","ดึงตัวเองเข้าหาโลหะแล้วดีดออก","ข้ามช่องว่าง, ดีดขึ้นที่สูง"),
        C(M,W,"โลหะเปียก","ของที่ถือเปียกและเย็น","วางทับดับไฟ, หล่อเย็นเครื่องที่ร้อนเกิน"),
        C(M,E,"แบตเตอรี่พกพา","ของที่ถือเก็บประจุ","วางในช่องเพื่อจ่ายไฟเครื่องที่อยู่ไกล"),
        C(M,H,"เหล็กเผา","ของที่ถือร้อนแดง","วางบนน้ำแข็ง ฟาง หรือขี้ผึ้ง"),
        C(P,M,"ยิงหมุดเหล็ก","ยิงเศษเหล็กปักผิวไม้","สร้างจุดยึดให้แม่เหล็กดึงตัวเองไป"),
        C(P,B,"ลูกลมกระดอน","ลมเป็นลูกเด้งกำแพง","กดสวิตช์ที่อยู่หลังมุม"),
        C(P,W,"ฉีดน้ำแรงสูง","น้ำแรง ระยะไกล","ดับไฟไกล, ผลักของหนัก, ล้างโคลน"),
        C(P,E,"ลมฟ้าผ่า","ประกายกระโดดต่อหลายเป้า","จ่ายไฟหลายเครื่องพร้อมกัน"),
        C(P,H,"ลมร้อน","ลมร้อนเป่าขึ้น","ยกตัว BLOB, เผาฟางที่อยู่ไกล"),
        C(B,M,"เด้งเกาะเหล็ก","ลงแล้วติดผนังหรือเพดานโลหะ","ปีนทางตั้งฉาก"),
        C(B,P,"พุ่งแรงดัน","พุ่งไปข้างหน้ากลางอากาศ","ข้ามคลองหรือช่องกว้าง"),
        C(B,W,"ตกน้ำกระจาย","ลงพื้นแล้วน้ำกระจายเป็นวง","รดพืชทั้งแปลง, ทำพื้นเปียกให้นำไฟ"),
        C(B,E,"ตกฟ้าผ่า","ลงพื้นแล้วช็อตรอบตัว","เปิดแผ่นกดไฟฟ้าหลายแผ่น"),
        C(B,H,"ตกอุกกาบาต","ลงพื้นร้อนจัด","ทะลุพื้นน้ำแข็ง, จุดฟางรอบตัว"),
        C(W,M,"สายน้ำแม่เหล็ก","สายน้ำพาเศษเหล็กลอยน้ำ","ลากแพหรือทุ่นเหล็กข้ามคลอง"),
        C(W,P,"สปริงเกลอร์","ฉีดละอองกว้าง","รดหลายต้นพร้อมกัน, ลดความร้อนในห้อง"),
        C(W,B,"ลูกน้ำเด้ง","ลูกน้ำกระดอน","ส่งน้ำข้ามกำแพงไปพืชบนที่สูง"),
        C(W,E,"สายน้ำไฟฟ้า","ช็อตผ่านสายน้ำ","จ่ายไฟให้วงจรที่จมน้ำ"),
        C(W,H,"ไอน้ำ","พ่นเมฆไอน้ำ","ยกตัว, หมุนกังหันไอน้ำ"),
        C(E,M,"แม่เหล็กไฟฟ้า","แม่เหล็กแรงขึ้นชั่วคราว (ถึง 40 kg)","ยกของหนักที่ปกติยกไม่ได้"),
        C(E,P,"ประกายยิงไกล","ไฟฟ้าระยะเพิ่ม 2 เท่า","จ่ายไฟเครื่องที่อยู่ไกล"),
        C(E,B,"ประกายกระดอน","ไฟฟ้าเด้งต่อ 3 เป้า","เปิดสวิตช์ตามลำดับ"),
        C(E,W,"ช็อตผ่านน้ำ","ไฟวิ่งทั่วแอ่งน้ำ","จ่ายไฟทุกอย่างที่แช่น้ำ"),
        C(E,H,"ไฟลัดวงจร","ฟิวส์หรือสายไฟร้อนจัด","ละลายฟิวส์ล็อก, เชื่อมสายไฟที่ขาด"),
        C(H,M,"ความร้อนเหนี่ยวนำ","ทำให้โลหะไกล ๆ ร้อนแดง","ละลายน้ำแข็งที่เอื้อมไม่ถึง"),
        C(H,P,"พ่นไฟ","ไฟไปไกลเป็นทาง","เผาฟางทั้งแนว"),
        C(H,B,"ลูกไฟเด้ง","ลูกไฟกระดอน","จุดเตาหรือคบไฟหลังกำแพง"),
        C(H,W,"ต้มเป็นไอ","ต้มน้ำเป็นไอ","ยกพื้นลอยแบบไกเซอร์, ขับเครื่องไอน้ำ"),
        C(H,E,"เชื่อมโลหะ","หลอมโลหะเข้าด้วยกัน","ซ่อมรางหรือสะพานเหล็ก"),
    };
}
}
