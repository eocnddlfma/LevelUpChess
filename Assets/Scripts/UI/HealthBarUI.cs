using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 체력바 UI 컴포넌트
    /// 기물의 체력을 시각적으로 표시
    /// 다이나믹 애니메이션: 
    /// - 피해 시: 빨간색 바가 먼저 줄고, 흰색 트레일이 천천히 따라감
    /// - 회복 시: 흰색 트레일이 먼저 늘고, 빨간색 바가 따라감
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;        // 실제 체력 (빨간색/초록색)
        [SerializeField] private Image trailImage;       // 트레일 바 (흰색) - fillImage 뒤에 위치
        
        [Header("수치 표시 UI")]
        [SerializeField] private TextMeshProUGUI healthText;    // 체력 수치 표시
        [SerializeField] private Image attackIcon;              // 공격력 아이콘 이미지
        [SerializeField] private TextMeshProUGUI attackText;    // 공격력 수치 표시
        
        [Header("색상 설정")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color trailColor = Color.white;
        [SerializeField] private Color healthTextColor = Color.white;
        [SerializeField] private Color attackTextColor = Color.yellow;
        
        [Header("애니메이션 설정")]
        [SerializeField] private float trailDelay = 0.3f;        // 트레일 애니메이션 시작 지연
        [SerializeField] private float trailSpeed = 2f;          // 트레일 애니메이션 속도
        [SerializeField] private float instantSpeed = 10f;       // 즉시 변화하는 바의 속도
        
        [Header("설정")]
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private bool showHealthNumbers = true;
        [SerializeField] private bool showAttackPower = true;
        
        private int maxHealth = 1;
        private int currentHealth = 1;
        private int attackPower = 1;
        
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        private float currentTrailAmount = 1f;
        
        private float trailDelayTimer = 0f;
        private bool isAnimating = false;
        private bool isDamage = false;  // true: 피해, false: 회복
        
        private void Start()
        {
            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;
            
            if (trailImage != null)
                trailImage.color = trailColor;
            
            if (healthText != null)
                healthText.color = healthTextColor;
            
            if (attackText != null)
                attackText.color = attackTextColor;
            
            UpdateHealthBarImmediate();
        }
        
        private void Update()
        {
            if (!isAnimating) return;
            
            AnimateHealthBar();
        }
        
        /// <summary>
        /// 체력바 초기화
        /// </summary>
        public void Initialize(int maxHp, int attack = 1)
        {
            maxHealth = Mathf.Max(1, maxHp);
            currentHealth = maxHealth;
            attackPower = attack;
            targetFillAmount = 1f;
            currentFillAmount = 1f;
            currentTrailAmount = 1f;
            UpdateHealthBarImmediate();
            UpdateStatsText();
        }
        
        /// <summary>
        /// 공격력 설정
        /// </summary>
        public void SetAttackPower(int attack)
        {
            attackPower = attack;
            UpdateStatsText();
        }
        
        /// <summary>
        /// 체력 설정
        /// </summary>
        public void SetHealth(int current, int max)
        {
            maxHealth = Mathf.Max(1, max);
            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(current, 0, maxHealth);
            
            float newTarget = (float)currentHealth / maxHealth;
            
            // 피해인지 회복인지 판단
            if (newTarget < targetFillAmount)
            {
                // 피해: 빨간색 바가 먼저 줄고, 흰색이 천천히 따라감
                isDamage = true;
                currentFillAmount = newTarget;  // 빨간색 바 즉시 업데이트
                // 흰색 트레일은 현재 위치 유지 후 천천히 따라감
            }
            else if (newTarget > targetFillAmount)
            {
                // 회복: 흰색 트레일이 먼저 늘고, 빨간색이 따라감
                isDamage = false;
                currentTrailAmount = newTarget;  // 흰색 트레일 즉시 업데이트
                // 빨간색 바는 현재 위치에서 천천히 따라감
            }
            
            targetFillAmount = newTarget;
            trailDelayTimer = trailDelay;
            isAnimating = true;
            
            UpdateHealthBarVisuals();
            UpdateStatsText();
        }
        
        /// <summary>
        /// 현재 체력만 업데이트
        /// </summary>
        public void SetCurrentHealth(int current)
        {
            SetHealth(current, maxHealth);
        }
        
        private void AnimateHealthBar()
        {
            bool animationComplete = true;
            
            if (isDamage)
            {
                // 피해 모드: 빨간색 바는 이미 목표에 도달, 흰색 트레일이 따라감
                // 딜레이 후 트레일 애니메이션
                if (trailDelayTimer > 0)
                {
                    trailDelayTimer -= Time.deltaTime;
                }
                else if (!Mathf.Approximately(currentTrailAmount, targetFillAmount))
                {
                    currentTrailAmount = Mathf.MoveTowards(currentTrailAmount, targetFillAmount, trailSpeed * Time.deltaTime);
                    animationComplete = false;
                }
            }
            else
            {
                // 회복 모드: 흰색 트레일은 이미 목표에 도달, 빨간색 바가 따라감
                // 딜레이 후 fill 애니메이션
                if (trailDelayTimer > 0)
                {
                    trailDelayTimer -= Time.deltaTime;
                }
                else if (!Mathf.Approximately(currentFillAmount, targetFillAmount))
                {
                    currentFillAmount = Mathf.MoveTowards(currentFillAmount, targetFillAmount, trailSpeed * Time.deltaTime);
                    animationComplete = false;
                }
            }
            
            UpdateHealthBarVisuals();
            
            if (animationComplete && trailDelayTimer <= 0)
            {
                isAnimating = false;
            }
        }
        
        private void UpdateHealthBarVisuals()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = currentFillAmount;
                
                // 체력에 따른 색상 변경
                float healthPercent = currentFillAmount;
                fillImage.color = healthPercent <= lowHealthThreshold 
                    ? lowHealthColor 
                    : Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
            
            if (trailImage != null)
            {
                trailImage.fillAmount = currentTrailAmount;
            }
        }
        
        private void UpdateStatsText()
        {
            if (healthText != null && showHealthNumbers)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
                healthText.gameObject.SetActive(true);
            }
            else if (healthText != null)
            {
                healthText.gameObject.SetActive(false);
            }
            
            if (showAttackPower)
            {
                if (attackIcon != null)
                    attackIcon.gameObject.SetActive(true);
                if (attackText != null)
                {
                    attackText.text = $"{attackPower}";
                    attackText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (attackIcon != null)
                    attackIcon.gameObject.SetActive(false);
                if (attackText != null)
                    attackText.gameObject.SetActive(false);
            }
        }
        
        private void UpdateHealthBarImmediate()
        {
            targetFillAmount = (float)currentHealth / maxHealth;
            currentFillAmount = targetFillAmount;
            currentTrailAmount = targetFillAmount;
            isAnimating = false;
            
            UpdateHealthBarVisuals();
            UpdateStatsText();
        }
        
        /// <summary>
        /// 체력바 표시/숨김
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// 수치 표시 설정
        /// </summary>
        public void SetShowNumbers(bool showHealth, bool showAttack)
        {
            showHealthNumbers = showHealth;
            showAttackPower = showAttack;
            UpdateStatsText();
        }
    }
}
