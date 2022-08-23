using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NameYourShip
{
    [BepInPlugin("fixingdeer.NameYourShip", "NameYourShip", "0.5.4")]
    [BepInProcess("valheim.exe")]

    public class NameYourShip : BaseUnityPlugin
    {
        //Change to false to stop logging
        public static bool debug = false;

        private readonly Harmony harmony = new Harmony("fixingdeer.NameYourShip");
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> requireKeyConfig;
        public static ConfigEntry<string> keyConfig;

        public static ConfigEntry<string> fontName;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<string> fontColor;
        //public static ConfigEntry<string> nameLocationLongship;
        //public static ConfigEntry<string> nameLocationKarve;
        //public static ConfigEntry<string> nameLocationRaft;
        public static ConfigEntry<Vector3> nameLocationLongship;
        public static ConfigEntry<Vector3> nameLocationKarve;
        public static ConfigEntry<Vector3> nameLocationRaft;
        public static ConfigEntry<float> distanceViewable;
        public static ConfigEntry<bool> fadeName;
        public static ConfigEntry<bool> scaleName;
        public static ConfigEntry<bool> showNameWhileOnShip;

        public static NameYourShip instance;

        public bool AllowInput()
        {
            if (!requireKeyConfig.Value) return true;
            if (Enum.TryParse(keyConfig.Value, out KeyCode key))
            {
                if (Input.GetKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        public void Awake()
        {
            instance = this;

            modEnabled = Config.Bind<bool>("General", "modEnabled", true, "Enable mod?");
            requireKeyConfig = Config.Bind<bool>("General", "requireKeyPress", false, "Require holding <editKey> to allow interaction");
            keyConfig = Config.Bind<string>("General", "editKey", "RightControl", "Key to be held to allow interaction");

            fontName = Config.Bind<string>("Ship Name Configuration", "fontName", "Norsebold", "Font to use when displaying the ship's name (must be a font included in the game resources)");
            fontSize = Config.Bind<int>("Ship Name Configuration", "fontSize", 48, "Font size to use when displaying the ship's name");
            fontColor = Config.Bind<string>("Ship Name Configuration", "fontColor", "#FFFFFFFF", "RGBA Font color to use when displaying the ship's name");
            //nameLocationLongship = Config.Bind<string>("Ship Name Configuration", "nameLocationLongship", "0, 600, 0", "The location to display the ship's name on a Longship, in relation to the ship's 0, 0, 0 point (must include x, y, z values)");
            //nameLocationKarve = Config.Bind<string>("Ship Name Configuration", "nameLocationKarve", "0, 600, 0", "The location to display the ship's name on a Karve, in relation to the ship's 0, 0, 0 point (must include x, y, z values)");
            //nameLocationRaft = Config.Bind<string>("Ship Name Configuration", "nameLocationRaft", "0, 200, 0", "The location to display the ship's name on a Raft, in relation to the ship's 0, 0, 0 point (must include x, y, z values)");
            nameLocationLongship = Config.Bind<Vector3>("Ship Name Configuration", "nameLocationLongship", new Vector3(0, 600, 0), "The location to display the ship's name on a Longship, in relation to the ship's 0, 0, 0 point (must include x, y, z values)");
            nameLocationKarve = Config.Bind<Vector3>("Ship Name Configuration", "nameLocationKarve", new Vector3(0, 600, 0), "The location to display the ship's name on a Karve, in relation to the ship's 0, 0, 0 point (must include x, y, z values)");
            nameLocationRaft = Config.Bind<Vector3>("Ship Name Configuration", "nameLocationRaft", new Vector3(0, 200, 0), "The location to display the ship's name on a Raft, in relation to the ship's 0, 0, 0 point (must include x, y, z values)");
            distanceViewable = Config.Bind<float>("Ship Name Configuration", "distanceViewable", 30, "How far away should you be able to see the name?");
            fadeName = Config.Bind<bool>("Ship Name Configuration", "fadeName", true, "Should the name fade out or just disappear all at once?");
            scaleName = Config.Bind<bool>("Ship Name Configuration", "scaleName", false, "Should the name get smaller as you get further away?");
            showNameWhileOnShip = Config.Bind<bool>("Ship Name Configuration", "showNameWhileOnShip", false, "Should the name be displayed while you're on the ship?");

            if (!modEnabled.Value)
                return;

            harmony.PatchAll();
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(Ship), "Awake")]
        public static class Ship_Awake_Patch
        {
            static void Postfix(ref Ship __instance)
            {
                if (modEnabled.Value)
                {
                    GameObject shipPlaceholderGO = new GameObject();
                    shipPlaceholderGO.name = "shipPlaceholderGO";
                    shipPlaceholderGO.transform.parent = __instance.gameObject.transform;
                    shipPlaceholderGO.transform.position = __instance.gameObject.transform.position;
                    shipPlaceholderGO.AddComponent<ShipPlaceholder>();

                    GameObject cylinder = shipPlaceholderGO.transform.Find("cylinder").gameObject;
                    cylinder.AddComponent<ShipNameSetter>();
                    cylinder.AddComponent<ShipName>();
                }
            }
        }
    }

    //public static class Vector3Extensions
    //{
    //    public static Vector3 StringToVector3(string sVector)
    //    {
    //        // Remove the parentheses
    //        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
    //        {
    //            sVector = sVector.Substring(1, sVector.Length - 2);
    //        }

    //        // split the items
    //        string[] sArray = sVector.Split(',');

    //        // store as a Vector3
    //        Vector3 result = new Vector3(
    //            float.Parse(sArray[0]),
    //            float.Parse(sArray[1]),
    //            float.Parse(sArray[2]));

    //        return result;
    //    }
    //}

    public class ShipName : MonoBehaviour
    {
        public Text shipNameText;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;

        public ShipNameSetter shipNameSetter;
        private static UnityEngine.Font signFont;

        void Awake()
        {
            UnityEngine.Font[] fonts = Resources.FindObjectsOfTypeAll<UnityEngine.Font>();
            foreach (UnityEngine.Font font in fonts)
            {
                if (font.name == NameYourShip.fontName.Value)
                {
                    signFont = font;
                    break;
                }
            }

            // Create Canvas GameObject.
            GameObject canvasGO = new GameObject();
            canvasGO.name = "Canvas";
            canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGO.transform.SetParent(gameObject.transform, false);

            // Get canvas from the GameObject.
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            //canvas.renderMode = RenderMode.WorldSpace;
            //canvas.worldCamera = Camera.main;

            // Create the Text GameObject.
            GameObject textGO = new GameObject();
            textGO.transform.parent = canvasGO.transform;
            shipNameText = textGO.AddComponent<Text>();

            // Create the ShipNameSetter GameObject.
            shipNameSetter = gameObject.GetComponent<ShipNameSetter>();

            // Set Text component properties.
            shipNameText.font = signFont;
            shipNameText.text = shipNameSetter.m_shipName;

            Color fontColor;
            if (ColorUtility.TryParseHtmlString(NameYourShip.fontColor.Value, out Color color))
                fontColor = color;
            else
                fontColor = Color.white;

            shipNameText.color = fontColor;
            shipNameText.fontSize = NameYourShip.fontSize.Value;
            shipNameText.alignment = TextAnchor.UpperCenter;

            // Provide Text position and size using RectTransform.
            rectTransform = shipNameText.GetComponent<RectTransform>();
        }


        bool IsVisible(Vector3 pos, Vector3 boundSize, Camera camera)
        {
            var bounds = new Bounds(pos, boundSize);
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }


        private float time = 0.0f;
        public float interpolationPeriod = 2f;

        void Update()
        {
            if (!Player.m_localPlayer) return;

            float distance = (gameObject.transform.position - Camera.main.transform.position).magnitude;
            float distanceViewable = NameYourShip.distanceViewable.Value;
            //string nameLocation = "0,0,0";
            Vector3 nameLocation = new Vector3(0, 0, 0);

            if (gameObject.transform.parent.gameObject.transform.parent.gameObject.name.Contains("VikingShip"))
            {
                nameLocation = NameYourShip.nameLocationLongship.Value;
            }
            else if (gameObject.transform.parent.gameObject.transform.parent.gameObject.name.Contains("Karve"))
            {
                nameLocation = NameYourShip.nameLocationKarve.Value;
            }
            else if (gameObject.transform.parent.gameObject.transform.parent.gameObject.name.Contains("Raft"))
            {
                nameLocation = NameYourShip.nameLocationRaft.Value;
            }

            bool shipVisible = IsVisible(gameObject.transform.position, gameObject.transform.GetComponent<Collider>().bounds.size, Camera.main);
            bool playerOnShip = false;

            Ship currentShip = Player.m_localPlayer.GetStandingOnShip();

            if (currentShip && currentShip.transform.GetInstanceID() == gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.GetInstanceID())
                playerOnShip = true;

            if (distance > distanceViewable || !shipVisible || (playerOnShip && !NameYourShip.showNameWhileOnShip.Value))
                shipNameText.gameObject.SetActive(false);
            else
                shipNameText.gameObject.SetActive(true);

            if (shipNameText.gameObject.activeSelf)
            {
                if (NameYourShip.fadeName.Value)
                {
                    float alpha = 1 - (distance - 2) / (distanceViewable - 2);
                    SetAlpha(alpha);
                }

                //Vector3 offset = Vector3Extensions.StringToVector3(nameLocation);
                Vector3 offset = nameLocation;
                Vector3 shipNamePos = Camera.main.WorldToScreenPoint(gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.position);

                float distanceFactor = distanceViewable * (1 - (distance / distanceViewable));
                float scaleFactor = 1 - (distance / distanceViewable);

                float distancePct = Mathf.Clamp(distance / distanceViewable, 0, 0.6f);
                shipNameText.transform.rotation = gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.rotation;
                shipNameText.transform.position = shipNamePos + new Vector3(offset.x, offset.y - (offset.y * distancePct), offset.z);

                if (NameYourShip.scaleName.Value)
                    shipNameText.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

                shipNameText.text = shipNameSetter.m_shipName;

                rectTransform.sizeDelta = new Vector2(shipNameText.text.Length * 100, shipNameText.fontSize * 2);
            }

            if (NameYourShip.debug)
            {
                //Only do this every [interpolationPeriod] seconds
                time += Time.deltaTime;

                if (time >= interpolationPeriod)
                {
                    time = time - interpolationPeriod;

                    // execute block of code here
                    //Debug.Log("gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.GetInstanceID() = " + gameObject.transform.parent.gameObject.transform.parent.gameObject.transform.GetInstanceID());
                    //Debug.Log("playerOnShip = " + playerOnShip);
                    //Debug.Log("NameYourShip.showNameWhileOnShip.Value = " + NameYourShip.showNameWhileOnShip.Value);

                    //if (currentShip)
                    //{
                    //    Debug.Log("ourShip.GetInstanceID() = " + currentShip.transform.GetInstanceID());
                    //}
                }
            }
        }


        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;

            if (alpha <= 0)
            {
                shipNameText.gameObject.SetActive(false);
            }
            else
            {
                shipNameText.gameObject.SetActive(true);
            }
        }
    }

    public class ShipPlaceholder : MonoBehaviour
    {
        public GameObject cylinder;

        void Awake()
        {
            cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "cylinder";
            cylinder.transform.parent = gameObject.transform;

            cylinder.GetComponent<Collider>().isTrigger = true;

            // Hide the cylinder from view
            cylinder.GetComponent<MeshRenderer>().enabled = false;
        }

        void PositionCylinder()
        {

            float height = 1f;
            float width = 1f;
            float forwardMove = 1f;

            if (gameObject.transform.parent.name.Contains("VikingShip"))
            {
                height = 1f;
                width = 2f;
                forwardMove = 2f;
            }
            else if (gameObject.transform.parent.name.Contains("Karve"))
            {
                height = 0.1f;
                width = 0.75f;
                forwardMove = 0.45f;
            }
            else if (gameObject.transform.parent.name.Contains("Raft"))
            {
                height = 0.75f;
                width = 1f;
                forwardMove = 1.5f;
            }

            Vector3 start = gameObject.transform.position;
            Vector3 end = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + height, gameObject.transform.position.z);

            var offset = end - start;
            var scale = new Vector3(width, offset.magnitude / 2.0f, width);
            var position = start + (offset / 2.0f);

            cylinder.transform.up = offset;
            cylinder.transform.localScale = scale;

            cylinder.transform.rotation = gameObject.transform.rotation;
            cylinder.transform.position = position + gameObject.transform.parent.gameObject.transform.forward * forwardMove;
        }

        void Update()
        {
            PositionCylinder();
        }
    }

    public class ShipNameSetter : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        public string m_shipName;
        public string m_name = "ShipName";
        public const int m_characterLimit = 100;
        public ZNetView m_nview;

        private void Awake()
        {
            m_nview = gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<ZNetView>();

            if (m_nview.GetZDO() == null)
            {
                return;
            }

            UpdateText();
            InvokeRepeating("UpdateText", 2f, 2f);
        }

        public string GetHoverText()
        {
            if (!NameYourShip.instance.AllowInput()) return string.Empty;

            if (!PrivateArea.CheckAccess(transform.position, 0f, false))
            {
                return "Restricted" + "\n" + GetText();
            }

            return Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] Set Ship Name") + "\n" + GetText();
        }

        public string GetHoverName()
        {
            if (!NameYourShip.instance.AllowInput()) return string.Empty;

            return "Test Text";
        }
        private void UpdateText()
        {
            string text = GetText();
            if (m_shipName == text)
            {
                return;
            }

            SetText(text);
        }

        public string GetText()
        {
            return m_nview.GetZDO().GetString("ShipName", string.Empty);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public void SetText(string text)
        {
            if (!PrivateArea.CheckAccess(transform.position, 0f, true))
            {
                return;
            }

            StartCoroutine(GetName(text, ApplyName));
        }

        private void ApplyName(string shipName)
        {
            m_nview.ClaimOwnership();
            m_nview.GetZDO().Set("ShipName", shipName);

            m_shipName = shipName;
        }

        public IEnumerator GetName(string text, Action<string> callback)
        {
            callback.Invoke(text);
            yield return null;
        }

        void Update()
        {
            this.transform.position = gameObject.transform.position;
        }

        bool Interactable.Interact(Humanoid user, bool hold, bool alt) {
            if (!NameYourShip.instance.AllowInput()) return false;
            if (hold) {
                return false;
            }
            if (!PrivateArea.CheckAccess(transform.position, 0f, true)) {
                return false;
            }
            TextInput.instance.RequestText(this, "$piece_sign_input", m_characterLimit);
            return true;
        }
    }
}