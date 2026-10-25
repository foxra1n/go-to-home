using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private UnityEngine.UI.Text scoreText;
        private RectTransform scoreBadge;
        private UnityEngine.UI.Text startCountdownText;
        private RectTransform startFrame;
        private GameObject gameOverPanel;
        private UnityEngine.UI.Text failText;
        private UnityEngine.UI.Text resultText;
        private UnityEngine.UI.Text retryText;

        public void Initialize()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var badge = new GameObject("ScoreBadge", typeof(RectTransform));
            badge.transform.SetParent(transform, false);
            scoreBadge = (RectTransform)badge.transform;
            scoreText = CreateText("Score", scoreBadge, font, 132, TextAnchor.MiddleCenter);
            var shadow = scoreText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(.035f, .045f, .065f, .9f);
            shadow.effectDistance = new Vector2(0, -8);
            var outline = scoreText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(.06f, .075f, .10f, 1f);
            outline.effectDistance = new Vector2(4, -4);
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.color = Color.white;
            scoreText.resizeTextForBestFit = true;
            scoreText.resizeTextMinSize = 1;
            scoreText.resizeTextMaxSize = 132;
            scoreText.horizontalOverflow = HorizontalWrapMode.Wrap;
            scoreText.rectTransform.anchorMin = new Vector2(.08f, .06f);
            scoreText.rectTransform.anchorMax = new Vector2(.92f, .94f);
            scoreText.rectTransform.offsetMin = scoreText.rectTransform.offsetMax = Vector2.zero;

            startCountdownText = CreateText("StartCountdown", transform, font, 148, TextAnchor.MiddleCenter);
            startCountdownText.fontStyle = FontStyle.Bold;
            startCountdownText.resizeTextForBestFit = true;
            startCountdownText.resizeTextMinSize = 1;
            startCountdownText.resizeTextMaxSize = 148;
            startCountdownText.horizontalOverflow = HorizontalWrapMode.Wrap;
            startCountdownText.color = new Color(1f, .98f, .88f);
            var startOutline = startCountdownText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            startOutline.effectColor = new Color(.055f, .12f, .16f);
            startOutline.effectDistance = new Vector2(3f, -3f);
            var startShadow = startCountdownText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            startShadow.effectColor = new Color(.025f, .07f, .10f, .85f);
            startShadow.effectDistance = new Vector2(0f, -6f);
            startFrame = CreateStartPlate();
            startFrame.SetSiblingIndex(startCountdownText.transform.GetSiblingIndex());
            SetRect(startCountdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 260f));

            gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            gameOverPanel.transform.SetParent(transform, false);
            RectTransform panelRect = (RectTransform)gameOverPanel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image panelImage = gameOverPanel.GetComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0.06f, 0.09f, 0.13f, 0.72f);
            panelImage.raycastTarget = false;

            failText = CreateText("Fail", gameOverPanel.transform, font, 148, TextAnchor.MiddleCenter);
            failText.fontStyle = FontStyle.Bold;
            failText.color = new Color(1f, 0.91f, 0.86f);
            SetRect(failText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 220f));
            failText.text = "FAIL";
            FitResultText(failText, .68f, .82f, 148);
            resultText = CreateText("ResultScores", gameOverPanel.transform, font, 76, TextAnchor.MiddleCenter);
            resultText.color = new Color(1f, .96f, .85f);
            resultText.fontStyle = FontStyle.Bold;
            FitResultText(resultText, .43f, .64f, 76);
            retryText = CreateText("RetryHint", gameOverPanel.transform, font, 42, TextAnchor.MiddleCenter);
            retryText.color = new Color(.86f, .9f, .91f);
            retryText.text = "TAP\u00a0TO\u00a0RETRY";
            FitResultText(retryText, .28f, .36f, 42);
            gameOverPanel.SetActive(false);
        }

        private static void FitResultText(UnityEngine.UI.Text text, float bottom, float top, int maxSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 1;
            text.resizeTextMaxSize = maxSize;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(.1f, bottom);
            rect.anchorMax = new Vector2(.9f, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private RectTransform CreateStartPlate()
        {
            var border = new GameObject("StartFrame", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            border.transform.SetParent(transform, false);
            var image = border.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(.055f, .12f, .16f);
            image.raycastTarget = false;
            var shadow = border.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(.035f, .07f, .10f, .75f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var face = new GameObject("TealFace", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            face.transform.SetParent(border.transform, false);
            var faceRect = (RectTransform)face.transform;
            faceRect.anchorMin = Vector2.zero; faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = new Vector2(6f, 6f); faceRect.offsetMax = new Vector2(-6f, -6f);
            face.GetComponent<UnityEngine.UI.Image>().color = new Color(.13f, .48f, .49f);
            face.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var rim = new GameObject("TopHighlight", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            rim.transform.SetParent(face.transform, false);
            var rimRect = (RectTransform)rim.transform;
            rimRect.anchorMin = new Vector2(0f, 1f); rimRect.anchorMax = Vector2.one;
            rimRect.pivot = new Vector2(.5f, 1f);
            rimRect.sizeDelta = new Vector2(0f, 5f); rimRect.anchoredPosition = Vector2.zero;
            rim.GetComponent<UnityEngine.UI.Image>().color = new Color(.62f, .86f, .78f);
            rim.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            border.SetActive(false);
            return (RectTransform)border.transform;
        }

        private void LateUpdate()
        {
            if (startCountdownText == null) return;
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            Camera camera = Camera.main;
            Rect viewport = camera != null ? camera.pixelRect : screen;
            Rect safe = Screen.safeArea;
            float left = Mathf.Max(viewport.xMin, safe.xMin);
            float right = Mathf.Min(viewport.xMax, safe.xMax);
            float bottom = Mathf.Max(viewport.yMin, safe.yMin);
            float top = Mathf.Min(viewport.yMax, safe.yMax);
            if (right <= left || top <= bottom) return;
            float badgeWidth = (right - left) * .46f;
            float badgeHeight = Mathf.Min((right - left) * .18f, (top - bottom) * .105f);
            float badgeTop = top - Mathf.Min((right - left) * .035f, (top - bottom) * .025f);
            float center = (left + right) * .5f;
            scoreBadge.anchorMin = new Vector2((center - badgeWidth * .5f) / screen.width, (badgeTop - badgeHeight) / screen.height);
            scoreBadge.anchorMax = new Vector2((center + badgeWidth * .5f) / screen.width, badgeTop / screen.height);
            scoreBadge.offsetMin = scoreBadge.offsetMax = Vector2.zero;
            RectTransform panel = (RectTransform)gameOverPanel.transform;
            panel.anchorMin = new Vector2(left / screen.width, bottom / screen.height);
            panel.anchorMax = new Vector2(right / screen.width, top / screen.height);
            panel.offsetMin = panel.offsetMax = Vector2.zero;

            // Overlay canvases span the window, even when the camera is letterboxed.
            float margin = (right - left) * 0.08f;
            RectTransform rect = startCountdownText.rectTransform;
            rect.anchorMin = new Vector2((left + margin) / screen.width,
                (bottom + (top - bottom) * 0.4f) / screen.height);
            rect.anchorMax = new Vector2((right - margin) / screen.width,
                (bottom + (top - bottom) * 0.6f) / screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            startFrame.anchorMin = new Vector2((left + margin) / screen.width,
                (bottom + (top - bottom) * .452f) / screen.height);
            startFrame.anchorMax = new Vector2((right - margin) / screen.width,
                (bottom + (top - bottom) * .548f) / screen.height);
            startFrame.offsetMin = startFrame.offsetMax = Vector2.zero;
            if (startFrame.gameObject.activeSelf)
            {
                rect.anchorMin = new Vector2((left + (right - left) * .14f) / screen.width,
                    (bottom + (top - bottom) * .463f) / screen.height);
                rect.anchorMax = new Vector2((right - (right - left) * .14f) / screen.width,
                    (bottom + (top - bottom) * .537f) / screen.height);
            }
        }

        public void SetScore(int score)
        {
            scoreText.text = score.ToString();
        }

        public void ShowPlaying()
        {
            startFrame.gameObject.SetActive(false);
            scoreBadge.gameObject.SetActive(true);
            startCountdownText.gameObject.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowWaitingToStart()
        {
            startFrame.gameObject.SetActive(true);
            scoreBadge.gameObject.SetActive(true);
            gameOverPanel.SetActive(false);
            startCountdownText.fontSize = 72;
            startCountdownText.resizeTextMaxSize = 72;
            startCountdownText.text = "TAP\u00a0TO\u00a0START";
            startCountdownText.gameObject.SetActive(true);
        }

        public void ShowCountdown(int count)
        {
            startFrame.gameObject.SetActive(false);
            scoreBadge.gameObject.SetActive(true);
            gameOverPanel.SetActive(false);
            startCountdownText.fontSize = 148;
            startCountdownText.resizeTextMaxSize = 148;
            startCountdownText.text = count.ToString();
            startCountdownText.gameObject.SetActive(true);
        }

        public void ShowGameOver(int score, string reason, int bestScore = 0)
        {
            startFrame.gameObject.SetActive(false);
            scoreBadge.gameObject.SetActive(false);
            resultText.text = "SCORE  " + score + "\nBEST  " + Mathf.Max(score, bestScore);
            startCountdownText.gameObject.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        private static UnityEngine.UI.Text CreateText(string objectName, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObject.transform.SetParent(parent, false);
            UnityEngine.UI.Text text = textObject.GetComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.06f, 0.09f, 0.13f);
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
