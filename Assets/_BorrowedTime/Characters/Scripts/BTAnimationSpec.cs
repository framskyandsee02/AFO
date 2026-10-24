using System;
namespace BorrowedTime.Characters {
[Serializable]public class BTAnimationSpec {
    public string name,label,category,face,returnTo,power;
    public int frames;
    public float duration;
    public bool loop;
}
[Serializable]public class BTAnimationCatalog {public BTAnimationSpec[] hero,robo;}
}
