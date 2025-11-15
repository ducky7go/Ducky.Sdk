using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Ducky.Sdk.Logging;

namespace Ducky.MessageHubUI;

/// <summary>
/// 左下角触发区域，鼠标悬停时从底部滑出小圆圈按钮
/// </summary>
public class TerminalHandlerView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("触发区域设置")]
    [SerializeField] private RectTransform? triggerArea;

    [Header("圆圈按钮")]
    [SerializeField] private GameObject? circleButton;
    [SerializeField] private RectTransform? circleButtonRect;
    [SerializeField] private Button? button;

    [Header("动画设置")]
    [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float hiddenYPosition = -100f; // 隐藏位置（屏幕外）
    [SerializeField] private float visibleYPosition = 50f;  // 可见位置

    private bool isMouseOver = false;
    private TerminalMainView? mainView;

    private void Awake()
    {
        Log.Info("[TerminalHandlerView] Awake() called");

        // 确保初始状态是隐藏的
        if (circleButtonRect != null)
        {
            var pos = circleButtonRect.anchoredPosition;
            pos.y = hiddenYPosition;
            circleButtonRect.anchoredPosition = pos;
            Log.Info($"[TerminalHandlerView] Circle button initial position: {pos}");
        }
    }

    private void Start()
    {
        Log.Info("[TerminalHandlerView] Start() called");

        // 在 Start() 中绑定按钮点击事件（此时所有字段都已赋值）
        if (button != null)
        {
            Log.Info("[TerminalHandlerView] Adding click listener to button");
            button.onClick.AddListener(OnCircleButtonClicked);
        }
        else
        {
            Log.Error("[TerminalHandlerView] Button is still null in Start!");
        }
    }

    public void Initialize(TerminalMainView mainView)
    {
        Log.Info($"[TerminalHandlerView] Initialize called with mainView={(mainView != null ? "valid" : "NULL")}");
        this.mainView = mainView;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 如果主面板已经展示，不响应触发区域
        if (mainView != null && mainView.IsVisible)
        {
            Log.Info("[TerminalHandlerView] Main panel is visible, ignoring trigger");
            return;
        }
        
        Log.Info("[TerminalHandlerView] Mouse entered trigger area");
        isMouseOver = true;
        StopAllCoroutines();
        StartCoroutine(SlideCircle(visibleYPosition));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Log.Info("[TerminalHandlerView] Mouse exited trigger area");
        isMouseOver = false;
        StopAllCoroutines();
        StartCoroutine(SlideCircle(hiddenYPosition));
    }
    
    /// <summary>
    /// 设置触发区域的激活状态
    /// </summary>
    public void SetActive(bool active)
    {
        if (triggerArea != null)
        {
            triggerArea.gameObject.SetActive(active);
            Log.Info($"[TerminalHandlerView] Trigger area set to {(active ? "active" : "inactive")}");
            
            // 如果禁用，确保圆圈按钮也隐藏
            if (!active && circleButtonRect != null)
            {
                StopAllCoroutines();
                var pos = circleButtonRect.anchoredPosition;
                pos.y = hiddenYPosition;
                circleButtonRect.anchoredPosition = pos;
            }
        }
    }

    private IEnumerator SlideCircle(float targetY)
    {
        if (circleButtonRect == null) yield break;

        while (Mathf.Abs(circleButtonRect.anchoredPosition.y - targetY) > 0.1f)
        {
            var pos = circleButtonRect.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * slideSpeed);
            circleButtonRect.anchoredPosition = pos;
            yield return null;
        }

        // 确保最终位置精确
        var finalPos = circleButtonRect.anchoredPosition;
        finalPos.y = targetY;
        circleButtonRect.anchoredPosition = finalPos;
    }

    private void OnCircleButtonClicked()
    {
        Log.Info("[TerminalHandlerView] Circle button clicked!");
        if (mainView != null)
        {
            Log.Info("[TerminalHandlerView] Calling mainView.Show()");
            mainView.Show();
        }
        else
        {
            Log.Error("[TerminalHandlerView] mainView is null!");
        }
    }

    /// <summary>
    /// 创建UI结构的静态工厂方法
    /// </summary>
    public static TerminalHandlerView Create(Canvas canvas, TerminalMainView mainView)
    {
        Log.Info("[TerminalHandlerView] Creating UI structure...");

        // 创建触发区域容器
        GameObject triggerObj = new GameObject("TerminalTriggerArea");
        triggerObj.transform.SetParent(canvas.transform, false);

        RectTransform triggerRect = triggerObj.AddComponent<RectTransform>();
        triggerRect.anchorMin = new Vector2(0, 0); // 左下角
        triggerRect.anchorMax = new Vector2(0, 0);
        triggerRect.pivot = new Vector2(0, 0);
        triggerRect.anchoredPosition = Vector2.zero;
        triggerRect.sizeDelta = new Vector2(150, 150); // 触发区域大小

        // 添加Image组件使其可以接收鼠标事件（但完全透明）
        Image triggerImage = triggerObj.AddComponent<Image>();
        triggerImage.color = new Color(0, 0, 0, 0); // 完全透明
        triggerImage.raycastTarget = true; // 确保可以接收射线检测

        Log.Info("[TerminalHandlerView] Trigger area created");

        // 创建圆圈按钮
        GameObject circleObj = new GameObject("CircleButton");
        circleObj.transform.SetParent(triggerObj.transform, false);

        RectTransform circleRect = circleObj.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0.5f, 0);
        circleRect.anchorMax = new Vector2(0.5f, 0);
        circleRect.pivot = new Vector2(0.5f, 0.5f);
        circleRect.anchoredPosition = new Vector2(0, -100); // 初始隐藏
        circleRect.sizeDelta = new Vector2(60, 60); // 圆圈大小

        // 添加按钮组件
        Image circleImage = circleObj.AddComponent<Image>();
        circleImage.color = new Color(0.2f, 0.6f, 1f, 0.8f); // 蓝色半透明
        circleImage.raycastTarget = true; // 确保按钮可以接收点击

        Button button = circleObj.AddComponent<Button>();

        Log.Info("[TerminalHandlerView] Circle button created");

        // 使圆圈看起来像圆形（简单方式，可以用Sprite替代）
        // 注：实际使用时建议使用圆形Sprite

        // 添加主脚本
        TerminalHandlerView handler = triggerObj.AddComponent<TerminalHandlerView>();
        handler.triggerArea = triggerRect;
        handler.circleButton = circleObj;
        handler.circleButtonRect = circleRect;
        handler.button = button;
        handler.Initialize(mainView);

        Log.Info("[TerminalHandlerView] UI structure created successfully");
        return handler;
    }
}
