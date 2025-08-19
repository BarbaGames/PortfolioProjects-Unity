using UnityEngine;

namespace Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        private void Die()
        {
            Debug.Log("Player died!");
        }
    }
}