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
    public Sprite[] kick = new Sprite[10];
    public Sprite[] special = new Sprite[10];
    public Sprite[] dash = new Sprite[10];
    public Sprite[] backstep = new Sprite[10];
    public Sprite[] victory = new Sprite[10];
    public Sprite[] ultimate = new Sprite[10];
    public Sprite[] punch = new Sprite[10];
    public Sprite[] knockout = new Sprite[10];
    public Sprite[] Get(string state) => state switch {
        "Block" => block, "DoubleJump" => doubleJump, "Walk" => walk, "Jump" => jump, "Hurt" => hurt,
        "Push" => push, "Kick" => kick, "Smash" => special, "Special" => special, "Dash" => dash, "Backstep" => backstep, "Victory" => victory, "Ultimate" => ultimate, "Punch" => punch, "Knockout" => knockout, _ => walk
    };
}
