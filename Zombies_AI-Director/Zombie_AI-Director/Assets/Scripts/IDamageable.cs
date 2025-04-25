using Demo.Scripts.Runtime.Character;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    int Health { get; set; }
    //void TakeDamage(int damage);
    void TakeDamage(int damage, FPSController fpsController);
}
