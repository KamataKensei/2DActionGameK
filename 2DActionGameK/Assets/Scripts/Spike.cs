using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("ó^Ç¶ÇÈÉ_ÉÅÅ[ÉW")]
    [SerializeField] int damage = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();

        if (player == null)
        {
            return;
        }

        player.TakeDamage(damage);
    }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
