using UnityEngine;

[CreateAssetMenu(menuName = "Woodland/Scout Animations")]
public class ScoutAnimations : ScriptableObject
{
    public Sprite[] block = new Sprite[10];
    public Sprite[] doubleJump = new Sprite[10];
    public Sprite[] walk = new Sprite[10];
    public Sprite[] jump = new Sprite[10];
    public Sprite[] hurt = new Sprite[10];
    public Sprite[] push = new Sprite[10];
    public Sprite[] punch = new Sprite[10];
    public Sprite[] knockout = new Sprite[10];
    public Sprite[] Get(string state) => state switch {
        "Block" => block, "DoubleJump" => doubleJump, "Walk" => walk, "Jump" => jump, "Hurt" => hurt,
        "Push" => push, "Punch" => punch, "Knockout" => knockout, _ => walk
    };
}
