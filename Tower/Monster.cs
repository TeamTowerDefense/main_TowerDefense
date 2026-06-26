using UnityEngine;

public class Monster : MonoBehaviour
{
    // 임시 코드. Monster가 완성되면 삭제.
    [SerializeField]
    private int hp = 100;

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
