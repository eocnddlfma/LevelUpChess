using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
        [SerializeField] private Image shieldIcon;              // 보호막 아이콘 이미지
        [SerializeField] private TextMeshProUGUI levelText;     // 레벨 표시
        
        [Header("보호막바 UI 요소")]
        [SerializeField] private Image shieldFillImage;         // 보호막 바 채움
        [SerializeField] private Image shieldTrailImage;        // 보호막 트레일 바
        
        [Header("색상 설정")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color trailColor = Color.white;
        [SerializeField] private Color healthTextColor = Color.white;
        [SerializeField] private Color attackTextColor = Color.yellow;
        [SerializeField] private Color shieldBarColor = new Color(0.3f, 0.3f, 0.3f, 1f); // 진한 회색
        [SerializeField] private Color levelTextColor = Color.cyan;
        [SerializeField] private Color expBarColor = new Color(0.3f, 0.7f, 1f, 1f);  // 하늘색
        [SerializeField] private Color expBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        
        [Header("애니메이션 설정")]
        [SerializeField] private float trailDelay = 0.05f;        // 트레일 애니메이션 시작 지연
        [SerializeField] private float trailDuration = 0.2f;     // 트레일 애니메이션 지속시간
        [SerializeField] private float fillDuration = 0.05f;     // 즉시 변화하는 바 지속시간
        [SerializeField] private Ease trailEase = Ease.OutQuad;  // 트레일 이징
        [SerializeField] private Ease fillEase = Ease.OutQuad;   // 필 이징
        
        [Header("설정")]
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private bool showHealthNumbers = true;
        [SerializeField] private bool showAttackPower = true;
        [SerializeField] private bool showShield = true;
        [SerializeField] private bool showLevel = true;
        [SerializeField] private bool showExpBar = true;
        [SerializeField] private bool showExpNumbers = false;  // 경험치 수치 표시 여부
        
        private int maxHealth = 1;
        private int currentHealth = 1;
        private int attackPower = 1;
        private int shield = 0;
        private int level = 1;
        
        // 경험치 관련
        private int currentExp = 0;
        private int expToNextLevel = 100;
        
        // Shield 바 관련
        private float currentShieldFillAmount = 0f;
        private float currentShieldTrailAmount = 0f;
        
        // DOTween 시퀀스
        private Tweener fillTween;
        private Tweener trailTween;
        private Tweener expTween;
        private Tweener shieldFillTween;
        private Tweener shieldTrailTween;
        
        private float currentFillAmount = 1f;
        private float currentTrailAmount = 1f;
        
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
            
            if (shieldIcon != null)
                shieldIcon.color = shieldBarColor;
            
            if (levelText != null)
                levelText.color = levelTextColor;
            
            
            if (shieldFillImage != null)
                shieldFillImage.color = shieldBarColor;
            
            if (shieldTrailImage != null)
                shieldTrailImage.color = trailColor;
            
            // 경험치 바 색상 설정
            if (expBackgroundImage != null)
                expBackgroundImage.color = expBackgroundColor;
            
            if (expFillImage != null)
                expFillImage.color = expBarColor;
            
            UpdateHealthBarImmediate();
            UpdateExpBarImmediate();
        }
        
        private void OnDestroy()
        {
            // DOTween 정리
            fillTween?.Kill();
            trailTween?.Kill();
            expTween?.Kill();
        }
        
        /// <summary>
        /// 체력바 초기화
        /// </summary>
        public void Initialize(int maxHp, int attack = 1, int lvl = 1, int exp = 0, int expToNext = 100)
        {
            // 기존 트윈 정리
            fillTween?.Kill();
            trailTween?.Kill();
            expTween?.Kill();
            
            maxHealth = Mathf.Max(1, maxHp);
            currentHealth = maxHealth;
            attackPower = attack;
            level = lvl;
            currentExp = exp;
            expToNextLevel = Mathf.Max(1, expToNext);
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
        /// 보호막 설정
        /// </summary>
        public void SetShield(int shieldValue)
        {
            shield = shieldValue;
            UpdateStatsText();
            UpdateShieldBar();
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
            
            float newTarget = Mathf.Clamp01((float)currentExp / expToNextLevel);
            AnimateExpTo(newTarget);
            
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
            
            // 레벨업 시 바를 0에서 시작
            expTween?.Kill();
            if (expFillImage != null)
                expFillImage.fillAmount = 0f;
            
            float newTarget = (float)currentExp / expToNextLevel;
            AnimateExpTo(newTarget);
            
            UpdateStatsText();
            UpdateExpText();
        }
        
        /// <summary>
        /// 체력 설정
        /// </summary>
        public void SetHealth(int current, int max)
        {
            maxHealth = Mathf.Max(1, max);
            currentHealth = Mathf.Clamp(current, 0, maxHealth);
            
            float newTarget = (float)currentHealth / maxHealth;
            
            // 기존 트윈 정리
            fillTween?.Kill();
            trailTween?.Kill();
            
            // 피해인지 회복인지 판단
            if (newTarget < currentFillAmount)
            {
                // 피해: 빨간색 바가 먼저 줄고, 흰색이 천천히 따라감
                // Fill 즉시 변경
                fillTween = DOTween.To(() => currentFillAmount, x => {
                    currentFillAmount = x;
                    UpdateFillVisual();
                }, newTarget, fillDuration).SetEase(fillEase);
                
                // Trail은 딜레이 후 따라감
                trailTween = DOTween.To(() => currentTrailAmount, x => {
                    currentTrailAmount = x;
                    UpdateTrailVisual();
                }, newTarget, trailDuration).SetEase(trailEase).SetDelay(trailDelay);
            }
            else if (newTarget > currentFillAmount)
            {
                // 회복: 흰색 트레일이 먼저 늘고, 빨간색이 따라감
                // Trail 즉시 변경
                trailTween = DOTween.To(() => currentTrailAmount, x => {
                    currentTrailAmount = x;
                    UpdateTrailVisual();
                }, newTarget, fillDuration).SetEase(fillEase);
                
                // Fill은 딜레이 후 따라감
                fillTween = DOTween.To(() => currentFillAmount, x => {
                    currentFillAmount = x;
                    UpdateFillVisual();
                }, newTarget, trailDuration).SetEase(trailEase).SetDelay(trailDelay);
            }
            
            UpdateStatsText();
        }
        
        /// <summary>
        /// 현재 체력만 업데이트
        /// </summary>
        public void SetCurrentHealth(int current)
        {
            SetHealth(current, maxHealth);
        }
        
        private void UpdateFillVisual()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = currentFillAmount;
                
                // 체력에 따른 색상 변경
                fillImage.color = currentFillAmount <= lowHealthThreshold 
                    ? lowHealthColor 
                    : Color.Lerp(lowHealthColor, fullHealthColor, currentFillAmount);
            }
        }
        
        private void UpdateTrailVisual()
        {
            if (trailImage != null)
            {
                trailImage.fillAmount = currentTrailAmount;
            }
        }
        
        private void UpdateHealthBarVisuals()
        {
            UpdateFillVisual();
            UpdateTrailVisual();
        }
        
        private void AnimateExpTo(float target)
        {
            expTween?.Kill();
            if (expFillImage != null)
                expTween = expFillImage.DOFillAmount(target, trailDuration).SetEase(trailEase);
        }
        
        private void UpdateExpBarImmediate()
        {
            expTween?.Kill();
            float target = (float)currentExp / expToNextLevel;
            if (expFillImage != null)
                expFillImage.fillAmount = target;
            
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
            
            // 보호막 표시 (아이콘만)
            if (showShield && shield > 0)
            {
                if (shieldIcon != null)
                    shieldIcon.gameObject.SetActive(true);
            }
            else
            {
                if (shieldIcon != null)
                    shieldIcon.gameObject.SetActive(false);
            }
            
            // 보호막 바 업데이트 (최대 체력에 비례)
            UpdateShieldBar();
            
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
            UpdateBar(fillImage, trailImage, ref currentFillAmount, ref currentTrailAmount, (float)currentHealth / maxHealth, ref fillTween, ref trailTween);
            UpdateStatsText();
        }
        
        /// <summary>
        /// 보호막 바 업데이트 (최대 체력에 비례)
        /// </summary>
        private void UpdateShieldBar()
        {
            UpdateBar(shieldFillImage, shieldTrailImage, ref currentShieldFillAmount, ref currentShieldTrailAmount, Mathf.Clamp01((float)shield / maxHealth), ref shieldFillTween, ref shieldTrailTween);
        }
        
        /// <summary>
        /// 바 업데이트 공통 메소드
        /// </summary>
        private void UpdateBar(Image fillImg, Image trailImg, ref float currentFill, ref float currentTrail, float target, ref Tweener fillTw, ref Tweener trailTw)
        {
            fillTw?.Kill();
            trailTw?.Kill();
            
            currentFill = target;
            currentTrail = target;
            
            if (fillImg != null)
                fillImg.fillAmount = currentFill;
            
            if (trailImg != null)
                trailImg.fillAmount = currentTrail;
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
        public void SetShowNumbers(bool showHealth, bool showAttack, bool showShield = true)
        {
            showHealthNumbers = showHealth;
            showAttackPower = showAttack;
            this.showShield = showShield;
            UpdateStatsText();
        }
    }
}
