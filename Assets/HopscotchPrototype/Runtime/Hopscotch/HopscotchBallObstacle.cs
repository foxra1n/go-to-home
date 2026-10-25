using UnityEngine;

namespace Homeward.Hopscotch
{
    // One reusable, scripted ball. Landing checks share the visible trajectory.
    public sealed class HopscotchBallObstacle : MonoBehaviour
    {
        public const float RollSpeed = 12f;
        public const float LandingHalfWidth = 1.1f;
        private Transform ball;
        private Material ballMaterial;
        private Texture2D soccerTexture;
        private double crossingTime;
        private double approachStartTime;
        private const float PreviewX = 2.8f;
        private const double ApproachEndLead = .15d;
        private Vector3 landing;
        private float speedMultiplier = 1f;
        private float approachStartX = PreviewX;
        public static bool BlocksLanding(double landingTime, double crossingTime)
            => System.Math.Abs((landingTime - crossingTime) * RollSpeed) <= LandingHalfWidth;

        public void Initialize()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            soccerTexture = CreateSoccerTexture();
            ballMaterial = new Material(shader) { color = Color.white, mainTexture = soccerTexture };
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "RollingSchoolBall";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * .68f;
            sphere.GetComponent<Renderer>().sharedMaterial = ballMaterial;
            RemoveCollider(sphere);
            ball = sphere.transform;
            Hide();
        }

        private static Texture2D CreateSoccerTexture()
        {
            // Twelve flat black pentagons on a white sphere; no protruding geometry.
            const int width = 512, height = 256;
            float phi = (1f + Mathf.Sqrt(5f)) * .5f;
            var centers = new Vector3[12];
            int index = 0;
            foreach (float a in new[] { -1f, 1f })
                foreach (float b in new[] { -1f, 1f })
                {
                    centers[index++] = new Vector3(0f, a, b * phi).normalized;
                    centers[index++] = new Vector3(a, b * phi, 0f).normalized;
                    centers[index++] = new Vector3(b * phi, 0f, a).normalized;
                }
            var tangent = new Vector3[12];
            var bitangent = new Vector3[12];
            for (int i = 0; i < 12; i++)
            {
                tangent[i] = Vector3.Cross(centers[i], Vector3.up).normalized;
                bitangent[i] = Vector3.Cross(centers[i], tangent[i]);
            }
            var pixels = new Color32[width * height];
            var white = new Color32(244, 242, 228, 255);
            var black = new Color32(25, 29, 34, 255);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float latitude = ((y + .5f) / height - .5f) * Mathf.PI;
                    float longitude = (x + .5f) / width * 2f * Mathf.PI;
                    var direction = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude),
                        Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));
                    Color32 color = white;
                    for (int i = 0; i < 12; i++)
                    {
                        float dot = Vector3.Dot(direction, centers[i]);
                        if (dot < .93f) continue;
                        float u = Vector3.Dot(direction, tangent[i]) / dot;
                        float v = Vector3.Dot(direction, bitangent[i]) / dot;
                        float angle = Mathf.Repeat(Mathf.Atan2(v, u) + Mathf.PI / 5f, 2f * Mathf.PI / 5f) - Mathf.PI / 5f;
                        float edge = .32f * Mathf.Cos(Mathf.PI / 5f) / Mathf.Cos(angle);
                        if (u * u + v * v <= edge * edge) { color = black; break; }
                    }
                    pixels[y * width + x] = color;
                }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true) { name = "SoccerBallPanels", wrapMode = TextureWrapMode.Repeat };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        public void Schedule(Vector3 target, double centerTime, float speed = 1f)
        {
            approachStartX = gameObject.activeSelf && Mathf.Abs(landing.z - target.z) < .01f
                ? Mathf.Clamp(ball.position.x, (float)ApproachEndLead * RollSpeed, PreviewX) : PreviewX;
            speedMultiplier = speed;
            landing = target;
            crossingTime = centerTime;
            approachStartTime = AudioSettings.dspTime;
            gameObject.SetActive(true);
        }

        public void Preview(Vector3 target)
        {
            landing = target;
            crossingTime = double.PositiveInfinity;
            gameObject.SetActive(true);
            Show(AudioSettings.dspTime);
        }

        public void Show(double now)
        {
            // Approach from the camera-visible side, rather than behind the student.
            float x = -Mathf.Max((float)(now - crossingTime) * RollSpeed * speedMultiplier, -PreviewX);
            if (double.IsPositiveInfinity(crossingTime)) x = PreviewX;
            else if (now < crossingTime - ApproachEndLead / speedMultiplier)
            {
                // Ease toward the lane only outside the unchanged landing danger zone.
                double end = crossingTime - ApproachEndLead / speedMultiplier;
                float progress = (float)((now - approachStartTime) / System.Math.Max(.001d, end - approachStartTime));
                x = Mathf.Lerp(approachStartX, (float)ApproachEndLead * RollSpeed, Mathf.Clamp01(progress));
            }
            ball.position = new Vector3(x, .43f, landing.z);
            ball.rotation = Quaternion.Euler(12f, 0f, -x / .34f * Mathf.Rad2Deg);
            ball.gameObject.SetActive(true);
            Camera camera = Camera.main;
            if (x < 0f && ((camera != null && camera.WorldToViewportPoint(ball.position).x < -.15f) || x < -40f))
                Hide();
        }

        public bool Blocks(double time) => BlocksLanding(crossingTime + (time - crossingTime) * speedMultiplier, crossingTime);
        public void Hide() => gameObject.SetActive(false);
        private static void RemoveCollider(GameObject obj)
        {
            Collider collider = obj.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
        }
        private void OnDestroy() { Destroy(ballMaterial); Destroy(soccerTexture); }
    }
}
