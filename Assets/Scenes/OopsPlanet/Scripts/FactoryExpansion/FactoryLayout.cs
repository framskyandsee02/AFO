using UnityEngine;
namespace OopsPlanet {
public static class FactoryLayout {
    public static readonly string[] Rooms={"Receiving","Central","Crane","Boiler","Cooling","Turbine","Assembly"};
    public static Vector3 Offset(string room){switch(room){case "Central":return new Vector3(0,0,20);case "Crane":return new Vector3(23,0,23);case "Boiler":return new Vector3(23,0,45);case "Cooling":return new Vector3(-23,0,21);case "Turbine":return new Vector3(-23,0,43);case "Assembly":return new Vector3(15,0,86.4f);default:return Vector3.zero;}}
    public static Vector3 Map(Vector3 old,string room)=>old+Offset(room);
}
}
