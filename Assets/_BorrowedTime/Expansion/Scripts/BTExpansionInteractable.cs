using UnityEngine;
namespace BorrowedTime {
public enum BTExpansionRole {Valve,Chime,Submit,Reset,Clue,Reward}
public class BTExpansionInteractable : MonoBehaviour {public BTExpansionWorld world;public int puzzle,slot;public BTExpansionRole role;public string label;}
}
