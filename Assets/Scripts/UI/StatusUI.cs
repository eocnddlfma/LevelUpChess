using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LevelUpChess.UI
{
    /// <summary>
    /// 상태 UI 컴포넌트
    /// 기물의 체력, 공격력, 레벨을 시각적으로 표시
    /// 다이나믹 애니메이션: 
    /// - 피해 시: 빨간색 바가 먼저 줄고, 흰색 트레일이 천천히 따라감
    /// - 회복 시: 흰색 트레일이 먼저 늘고, 빨간색 바가 따라감
    /// </summary>
    public class StatusUI : MonoBehaviour
    {
        [Header("체력바 UI 요소")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;        // 실제 체력 (빨간색/초록색)
        [SerializeField] private Image trailImage;       // 트레일 바 (흰색) - fillImage 뒤에 위치
        
        [Header("경험치바 UI 요소")]
        [SerializeField] private Image expBackgroundImage;  // 경험치 바 배경
        [SerializeField] private Image expFillImage;        // 경험치 바 채움
        [SerializeField] private TextMeshProUGUI expText;   // 경험치 수치 표시 (선택)
        
        [Header("수치 표시 UI")]
        [SerializeField] private TextMeshProUGUI healthText;    // 체력 수치 표시
        [SerializeField] private Image attackIcon;              // 공격력 아이콘 이미지
        [SerializeField] private TextMeshProUGUI attackText;    // 공격력 수치 표시
        [SerializeField] private TextMeshProUGUI levelText;     // 레벨 표시
        
        [Header("색상 설정")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color trailColor = Color.white;
        [SerializeField] private Color healthTextColor = Color.white;
        [SerializeField] private Color attackTextColor = Color.yellow;
        [SerializeField] private Color levelTextColor = Color.cyan;
        [SerializeField] private Color expBarColor = new Color(0.3f, 0.7f, 1f, 1f);  // 하늘색
        [SerializeField] private Color expBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        
        [Header("애니메이션 설정")]
        [SerializeField] private float trailDelay = 0.3f;        // 트레일 애니메이션 시작 지연
        [SerializeField] private float trailSpeed = 2f;          // 트레일 애니메이션 속도
        [SerializeField] private float instantSpeed = 10f;       // 즉시 변화하는 바의 속도
        
        [Header("설정")]
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private bool showHealthNumbers = true;
        [SerializeField] private bool showAttackPower = true;
        [SerializeField] private bool showLevel = true;
        [SerializeField] private bool showExpBar = true;
        [SerializeField] private bool showExpNumbers = false;  // 경험치 수치 표시 여부
        
        private int maxHealth = 1;
        private int currentHealth = 1;
        private int attackPower = 1;
        private int level = 1;
        
        // 경험치 관련
        private int currentExp = 0;
        private int expToNextLevel = 100;
        private float targetExpFillAmount = 0f;
        private float currentExpFillAmount = 0f;
        private bool isExpAnimating = false;
        
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
            
            if (levelText != null)
                levelText.color = levelTextColor;
            
            // 경험치 바 색상 설정
            if (expBackgroundImage != null)
                expBackgroundImage.color = expBackgroundColor;
            
            if (expFillImage != null)
                expFillImage.color = expBarColor;
            
            UpdateHealthBarImmediate();
            UpdateExpBarImmediate();
        }
        
        private void Update()
        {
            if (isAnimating)
            {
                AnimateHealthBar();
            }
            
            if (isExpAnimating)
            {
                AnimateExpBar();
            }
        }
        
        /// <summary>
        /// 체력바 초기화
        /// </summary>
        public void Initialize(int maxHp, int attack = 1, int lvl = 1, int exp = 0, int expToNext = 100)
        {
            maxHealth = Mathf.Max(1, maxHp);
            currentHealth = maxHealth;
            attackPower = attack;
            level = lvl;
            currentExp = exp;
            expToNextLevel = Mathf.Max(1, expToNext);
            targetFillAmount = 1f;
            currentFillAmount = 1f;
            currentTrailAmount = 1f;
            UpdateHealthBarImmediate();
            UpdateExpBarImmediate();
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
        /// 레벨 설정
        /// </summary>
        public void SetLevel(int lvl)
        {
            level = lvl;
            UpdateStatsText();
        }
        
        /// <summary>
        /// 경험치 설정
        /// </summary>
        public void SetExperience(int current, int toNextLevel)
        {
            currentExp = Mathf.Max(0, current);
            expToNextLevel = Mathf.Max(1, toNextLevel);
            
            float newTarget = (float)currentExp / expToNextLevel;
            targetExpFillAmount = Mathf.Clamp01(newTarget);
            isExpAnimating = true;
            
            UpdateExpText();
        }
        
        /// <summary>
        /// 레벨업 시 경험치 바 리셋 (애니메이션 포함)
        /// </summary>
        public void OnLevelUp(int newLevel, int remainingExp, int newExpToNextLevel)
        {
            level = newLevel;
            currentExp = remainingExp;
            expToNextLevel = Mathf.Max(1, newExpToNextLevel);
            
            // 레벨업 시 바를 꽉 채웠다가 리셋
            currentExpFillAmount = 0f;
            targetExpFillAmount = (float)currentExp / expToNextLevel;
            isExpAnimating = true;
            
            UpdateStatsText();
            UpdateExpText();
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
        
        private void AnimateExpBar()
        {
            if (!Mathf.Approximately(currentExpFillAmount, targetExpFillAmount))
            {
                currentExpFillAmount = Mathf.MoveTowards(currentExpFillAmount, targetExpFillAmount, trailSpeed * Time.deltaTime);
                UpdateExpBarVisuals();
            }
            else
            {
                isExpAnimating = false;
            }
        }
        
        private void UpdateExpBarVisuals()
        {
            if (expFillImage != null)
            {
                expFillImage.fillAmount = currentExpFillAmount;
            }
        }
        
        private void UpdateExpBarImmediate()
        {
            targetExpFillAmount = (float)currentExp / expToNextLevel;
            currentExpFillAmount = targetExpFillAmount;
            isExpAnimating = false;
            
            UpdateExpBarVisuals();
            UpdateExpText();
        }
        
        private void UpdateExpText()
        {
            if (expText != null)
            {
                if (showExpNumbers)
                {
                    expText.text = $"{currentExp}/{expToNextLevel}";
                    expText.gameObject.SetActive(true);
                }
                else
                {
                    expText.gameObject.SetActive(false);
                }
            }
            
            // 경험치 바 표시/숨김
            if (expBackgroundImage != null)
                expBackgroundImage.gameObject.SetActive(showExpBar);
            if (expFillImage != null)
                expFillImage.gameObject.SetActive(showExpBar);
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
            
            // 레벨 표시
            if (showLevel && levelText != null)
            {
                levelText.text = $"Lv.{level}";
                levelText.gameObject.SetActive(true);
            }
            else if (levelText != null)
            {
                levelText.gameObject.SetActive(false);
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
