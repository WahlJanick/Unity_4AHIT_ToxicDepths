using ToxicDepths.Mining;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ToxicDepths
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerMining), typeof(ShredderPlacement))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(120)]
    public sealed class ShredderShop : MonoBehaviour
    {
        private const int EmeraldSlot = 3;
        private const int StoneSlot = 4;
        private static readonly Color Panel = new Color(0.035f, 0.055f, 0.065f, 0.96f);
        private static readonly Color Accent = new Color(0.40f, 0.94f, 0.66f);

        [SerializeField, Min(0)] private int startingEmeralds = 6;
        [SerializeField] private Sprite emeraldIcon;
        [SerializeField] private Sprite stoneIcon;

        private PlayerMovement movement;
        private PlayerMining mining;
        private ShredderPlacement placement;
        private Shredder viewedMachine;
        private Rigidbody emeraldOutput;
        private Rigidbody stoneOutput;
        private bool isOpen;
        private int emeralds;
        private int stones;
        private string purchaseMessage;
        private GUIStyle heading;
        private GUIStyle line;
        private GUIStyle hint;

        internal Rigidbody SelectedOutput
        {
            get
            {
                var hotbar = placement.Hotbar;
                if (hotbar == null) return null;

                return hotbar.SelectedIndex switch
                {
                    EmeraldSlot when emeralds > 0 => emeraldOutput,
                    StoneSlot when stones > 0 => stoneOutput,
                    _ => null
                };
            }
        }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            mining = GetComponent<PlayerMining>();
            placement = GetComponent<ShredderPlacement>();
        }

        private void Start()
        {
            var shredder = placement.ShredderPrefab;
            if (shredder != null)
            {
                emeraldOutput = shredder.GetOutputPrefab(ShredderOutputPickup.Kind.Emerald);
                stoneOutput = shredder.GetOutputPrefab(ShredderOutputPickup.Kind.Stone);
            }

            emeralds = startingEmeralds;
            stones = 0;
            RefreshOutputSlots();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (isOpen)
            {
                if (keyboard == null) return;
                if (keyboard.bKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) SetOpen(false);
                else if (keyboard.digit2Key.wasPressedThisFrame) TryBuy(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) TryBuy(2);
                return;
            }

            viewedMachine = null;
            if (!movement.CanUseTools) return;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            {
                SetOpen(true);
                return;
            }

            if (!mining.TryAim(out RaycastHit hit)) return;
            viewedMachine = hit.collider.GetComponentInChildren<Shredder>();
            if (keyboard == null || !keyboard.eKey.wasPressedThisFrame || mining.IsCarryingOre)
                return;

            var body = hit.collider.attachedRigidbody;
            if (body == null || !body.TryGetComponent(out ShredderOutputPickup pickup) || !pickup.TryCollect())
                return;

            if (pickup.ItemKind == ShredderOutputPickup.Kind.Emerald) emeralds++;
            else stones++;
            RefreshOutputSlots();
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            viewedMachine = null;
            purchaseMessage = null;
            movement.SetShopOpen(open);
        }

        private void TryBuy(int model)
        {
            var item = Shredder.Models[model];
            if (emeralds < item.Price)
            {
                purchaseMessage = "Dafür brauchst du mehr Smaragde.";
                return;
            }
            if (!placement.GiveShredder(model)) return;

            emeralds -= item.Price;
            RefreshOutputSlots();
            purchaseMessage = $"Gekauft · {item.Name} liegt in Slot {model + 1}.";
        }

        private void RefreshOutputSlots()
        {
            var hotbar = placement.Hotbar;
            if (hotbar == null) return;
            hotbar.SetSlot(EmeraldSlot, emeraldIcon, emeralds);
            hotbar.SetSlot(StoneSlot, stoneIcon, stones);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!isOpen && viewedMachine == null) return;

            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
                line = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                hint = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                heading.normal.textColor = Accent;
                line.normal.textColor = Color.white;
                hint.normal.textColor = new Color(0.75f, 0.82f, 0.84f);
            }

            if (isOpen) DrawShop();
            else DrawProgress(viewedMachine);
        }

        private void DrawProgress(Shredder machine)
        {
            var panel = new Rect(Screen.width * 0.5f - 143f, Screen.height * 0.5f + 35f, 286f, 75f);
            DrawRect(panel, Panel);
            var model = Shredder.Models[machine.ModelIndex];
            GUI.Label(new Rect(panel.x + 12f, panel.y + 5f, 262f, 26f),
                $"Zerschredderer · {model.Name}", heading);

            float progress = machine.Progress;
            string status = machine.IsProcessing
                ? $"{Mathf.FloorToInt(progress * 100f)} % · {machine.SecondsRemaining:0.0} s"
                : "Bereit";
            GUI.Label(new Rect(panel.x + 12f, panel.y + 30f, 262f, 24f), status, line);
            var bar = new Rect(panel.x + 13f, panel.y + 59f, 260f, 5f);
            DrawRect(bar, new Color(0.17f, 0.24f, 0.25f));
            bar.width *= progress;
            DrawRect(bar, Accent);
        }

        private void DrawShop()
        {
            var panel = new Rect((Screen.width - 440f) * 0.5f, (Screen.height - 260f) * 0.5f, 440f, 260f);
            DrawRect(panel, Panel);
            DrawRect(new Rect(panel.x, panel.y, panel.width, 3f), Accent);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 15f, 404f, 28f), "Zerschredderer-Shop", heading);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 44f, 404f, 25f), $"Smaragde im Inventar   {emeralds}", line);

            for (int i = 1; i < Shredder.Models.Count; i++)
            {
                var item = Shredder.Models[i];
                float y = panel.y + 80f + (i - 1) * 51f;
                DrawRect(new Rect(panel.x + 18f, y, 404f, 43f), new Color(0.09f, 0.14f, 0.15f));
                GUI.Label(new Rect(panel.x + 28f, y + 7f, 382f, 29f),
                    $"[{i + 1}]  {item.Name}     {item.Seconds:0.0} s     {item.Price} Smaragde", line);
            }

            if (!string.IsNullOrEmpty(purchaseMessage))
                GUI.Label(new Rect(panel.x + 18f, panel.y + 181f, 404f, 26f), purchaseMessage, line);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 212f, 404f, 22f),
                "Smaragde mit E aufheben. Kaufen mit 2 oder 3.", hint);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 231f, 404f, 22f), "B oder Esc schließt den Shop.", hint);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OnDisable()
        {
            if (isOpen) SetOpen(false);
            viewedMachine = null;
        }
    }
}
