using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using AssetsLib;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Reflection.Emit;

namespace Summoner
{
    [BepInPlugin("Aidanamite.Summoner", "Summoner", "1.1.3")]
    public class Main : BaseUnityPlugin
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\BepInEx\\{modName}";
        static class Amulets
        {
            public static string Slime;
            public static string Mother;
            public static string GolemDungeon;
            public static string ForestDungeon;
            public static string DesertDungeon;
            public static string TeslaDungeon;
            public static string Mimic;
        }
        static class Armour
        {
            public static RefString[] Helmet = { "", "", "", "" };
            public static RefString[] Plate = { "", "", "", "" };
            public static RefString[] Boots = { "", "", "", "" };
        }

        static RefString[][] Recipes = {
            new RefString[] {
                "Hardened Steel",
                "Water Sphere",
                "Empowering Crystal"
            },
            new RefString[] {
                "",
                "Old Bulb",
                "Empowering Crystal"
            },
            new RefString[] {
                "",
                "Desert Steel Ingot",
                "Empowering Crystal"
            },
            new RefString[] {
                "",
                "Tungsten Reel",
                "Empowering Crystal"
            }
        };
        public static float GetFamiliarBoost
        {
            get
            {
                var m = 1f;
                if (HeroMerchant.Instance && HeroMerchant.Instance.heroMerchantInventory) {
                    var item = HeroMerchant.Instance.heroMerchantInventory.GetEquippedItemByType(HeroMerchantInventory.EquipmentSlot.Head);
                    if (item && Array.Exists(Armour.Helmet, (x) => x.s == item.master.name))
                        m -= 0.25f;
                    item = HeroMerchant.Instance.heroMerchantInventory.GetEquippedItemByType(HeroMerchantInventory.EquipmentSlot.Body);
                    if (item && Array.Exists(Armour.Plate, (x) => x.s == item.master.name))
                        m -= 0.4f;
                    item = HeroMerchant.Instance.heroMerchantInventory.GetEquippedItemByType(HeroMerchantInventory.EquipmentSlot.Boots);
                    if (item && Array.Exists(Armour.Boots, (x) => x.s == item.master.name))
                        m -= 0.3f;
                }
                return m;
            }
        }

        class RefString
        {
            public string s;
            public RefString(string value)
            {
                s = value;
            }
            public static implicit operator string(RefString obj) => obj.s;
            public static implicit operator RefString(string obj) => new RefString(obj);
        }

        void Awake()
        {
            Amulets.Slime = CreateSummonRing<SlimeAmuletEffect>("Slime", "Slime", AssetsLibTools.CreateStatModifier(Speed: 20));
            Amulets.Mother = CreateSummonRing<MotherAmuletEffect>("Mother", "Mother Golem", AssetsLibTools.CreateStatModifier(Defence: 10), Culture: ItemMaster.Culture.Desert);
            Amulets.GolemDungeon = CreateSummonRing<GolemDungeonAmuletEffect>("Golem", "Golem", AssetsLibTools.CreateStatModifier(), Culture: ItemMaster.Culture.Golem);
            Amulets.ForestDungeon = CreateSummonRing<ForestDungeonAmuletEffect>("Forest", "Forest", AssetsLibTools.CreateStatModifier(), Culture: ItemMaster.Culture.Forest);
            Amulets.DesertDungeon = CreateSummonRing<DesertDungeonAmuletEffect>("Desert", "Desert", AssetsLibTools.CreateStatModifier(), Culture: ItemMaster.Culture.Desert);
            Amulets.TeslaDungeon = CreateSummonRing<TeslaDungeonAmuletEffect>("Tesla", "Tesla", AssetsLibTools.CreateStatModifier(), Culture: ItemMaster.Culture.Tech);
            Amulets.Mimic = CreateSummonRing<MimicAmuletEffect>("Mimic", "Mimic", AssetsLibTools.CreateStatModifier(), new Dictionary<string, string> { ["default"] = "Ring that doubles the summoned familiars" });
            CreateArmourItems(ref Armour.Helmet, "Helmet", RecipeIds.Blacksmith.Helmets, EquipmentItemMaster.EquipmentSlot.Head);
            CreateArmourItems(ref Armour.Plate, "Chestplate", RecipeIds.Blacksmith.Chestplates, EquipmentItemMaster.EquipmentSlot.Body);
            CreateArmourItems(ref Armour.Boots, "Boots", RecipeIds.Blacksmith.Boots, EquipmentItemMaster.EquipmentSlot.Boots);
            new Harmony($"com.Aidanamite.{modName}").PatchAll(modAssembly);
            Logger.LogInfo($"{modName} has loaded");
        }

        public static string CreateSummonRing<T>(string Name, string CreatureName, StatsModificator StatModifier, Dictionary<string, string> Desc = null, ItemMaster.Culture Culture = ItemMaster.Culture.Merchant) where T : SummonAmuletEffect
        {
            var item = new AmuletEquipmentMaster();
            item.SetupBasicItem(Id: Name + "SummonAmulet",
                NameLocalizationKey: AssetsLibTools.RegisterLocalization(Name + "SummonAmuletName", new Dictionary<string, string> { ["default"] = $"Summoner's Amulet ({CreatureName})" }),
                DescriptionLocalizationKey: AssetsLibTools.RegisterLocalization(Name + "SummonAmuletDesc", Desc == null ? new Dictionary<string, string> { ["default"] = $"Ring that summons multiple {CreatureName.ToLower()} familiars" } : Desc),
                SpriteAssetName: AssetsLibTools.RegisterAsset(Name + "SummonAmuletSprite", AssetsLibTools.LoadImage($"summon_amulet_{Name.ToLower()}.png", 32, 32).CreateSprite()),
                Culture: Culture, SpawnWeight: 0
                );
            item.SetupEquipmentItem(StatModifier: StatModifier);
            item.SetupAmuletItem(EffectAssetName: AssetsLibTools.RegisterEffect<T>(),
                CanRespawnIfLost: true);
            item.RegisterItem();
            return item.name;
        }

        static void CreateArmourItems(ref RefString[] collection, string Name, string recipeId, EquipmentItemMaster.EquipmentSlot slot)
        {
            var art = CreateEmptyArtAsset($"SummonArmor{Name}Art");
            var name = AssetsLibTools.RegisterLocalization($"SummonArmour{Name}Name", new Dictionary<string, string> { ["default"] = "Summoner's " + Name });
            var desc = AssetsLibTools.RegisterLocalization($"SummonArmour{Name}Desc", new Dictionary<string, string> { ["default"] = "Reduces weapon damage but increases familiar damage" });
            var sprite = AssetsLibTools.RegisterAsset($"SummonArmour{Name}Sprite", AssetsLibTools.LoadImage($"summon_armour_{Name.ToLower()}.png", 32, 32).CreateSprite());
            for (int i = 0; i < 4; i++)
                collection[i].s = CreateArmourItem(Name, i, recipeId, slot, 4000, 3400, art, (i > 0) ? collection[i-1] : Recipes[i][0], Recipes[i][1], Recipes[i][2], name, desc, sprite);
        }

        static string CreateEmptyArtAsset(string Name)
        {
            var art = new GameObject(Name, new Type[] { typeof(Animator) });
            var artChild = new GameObject("InGameArt");
            artChild.transform.SetParent(art.transform, false);
            new GameObject("sprite", new Type[] { typeof(SpriteRenderer) }).transform.SetParent(artChild.transform, false);
            return AssetsLibTools.RegisterAsset(art.name, art);
        }

        public static string CreateArmourItem(string Name, int Level, string recipeId, EquipmentItemMaster.EquipmentSlot slot, int sellCost, int buyCost, string Art, string Item1, string Item2, string Item3, string NameKey, string DescKey, string SpriteKey)
        {
            var item = new EquipmentItemMaster();
            item.SetupBasicItem(Id: "SummonArmour" + Name + (Level > 0 ? (Level + 1).ToString() : ""),
                NameLocalizationKey: NameKey,
                DescriptionLocalizationKey: DescKey,
                SpriteAssetName: SpriteKey,
                GoldValue: (int)(sellCost * Mathf.Pow(2.5f, Level)));

            item.SetupEquipmentItem(AssetsLibTools.CreateStatModifier(Health: (int)(20 * Mathf.Pow(2.25f, Level)), Speed: 15, PlusLevelModifier: (x, y) => x.health *= (int)Mathf.Pow(10, y)), Art);
            item.equipmentSlot = slot;
            item.RegisterItem();
            AssetsLibTools.RegisterBlacksmithRecipeSet(recipeId,
                AssetsLibTools.CreateRecipe(item, new List<RecipeIngredient>() {
                    new RecipeIngredient(Item1, 1),
                    new RecipeIngredient(Item2, 2),
                    new RecipeIngredient(Item3, 4)
                }, buyCost * (1 + Level * 8), PriceIsFixed: false, UnlockedAtStart: Level == 0, SortingIndex: 1));
            AssetsLibTools.CreateAndRegisterEnchantmentRecipe(
                Item: item,
                StatModifier: AssetsLibTools.CreateStatModifier(Defence: 10),
                Cost: (int)(sellCost * Mathf.Pow(2.5f, Level) / 2),
                Ingredients: new List<RecipeIngredient>() {
                    new RecipeIngredient("Empowering Crystal",5)
                });
            return item.name;
        }

        void Update()
        {
            /*if (Input.GetKeyDown(KeyCode.Slash)) {
                var item = ItemDatabase.GetItemByName(Input.GetKey(KeyCode.Alpha1) ? Amulets.Slime : Input.GetKey(KeyCode.Alpha2) ? Amulets.Mother : Input.GetKey(KeyCode.Alpha3) ? Amulets.GolemDungeon : Input.GetKey(KeyCode.Alpha4) ? Amulets.ForestDungeon : Input.GetKey(KeyCode.Alpha5) ? Amulets.DesertDungeon : Input.GetKey(KeyCode.Alpha6) ? Amulets.TeslaDungeon : Input.GetKey(KeyCode.Alpha7) ? Amulets.Mimic : Input.GetKey(KeyCode.Alpha8) ? Armour.Helmet : Input.GetKey(KeyCode.Alpha9) ? Armour.Plate : Armour.Boots, GameManager.Instance.GetCurrentGamePlusLevel());
                CultureManager.Instance.DiscoverItem(item);
                HeroMerchant.Instance.heroMerchantInventory.TryAddItem(ItemStack.Create(item));
            }*/
            if (checkFamiliar)
            {
                checkFamiliar = false;
                EnsureFamiliars();
            }
        }

        public static List<SummonAmuletEffect> amulets = new List<SummonAmuletEffect>();
        public static bool checkFamiliar = false;
        public static int GetFamiliarCount(FamiliarBase.Familiars f, Amulet[] playerAmulets = null)
        {
            int c = FamiliarsManager.IsEquipped(f) ? 1 : 0;
            if (playerAmulets == null)
                playerAmulets = FindObjectsOfType<Amulet>();
            foreach (var e in playerAmulets)
                if (e.amuletEffect != null && e.amuletEffect is AddFamiliarAmuletEffect && (e.amuletEffect as AddFamiliarAmuletEffect).familiar == f)
                    c++;
            foreach (var a in amulets)
                foreach (var familiar in a.familiars)
                    if (familiar == f)
                        c++;
            if (amulets.Exists((x) => x.GetType() == typeof(MimicAmuletEffect)))
                return c * 2;
            return c;
        }
        public static void AddFamiliars(SummonAmuletEffect effect)
        {
            if (!amulets.Contains(effect))
                amulets.Add(effect);
            checkFamiliar = true;
        }
        public static void RemoveFamiliars(SummonAmuletEffect effect)
        {
            amulets.Remove(effect);
            checkFamiliar = true;
        }
        public static void EnsureFamiliars()
        {
            if (!HeroMerchant.Instance || !GameManager.Instance)
                return;
            bool flag = false;
            var a = FindObjectsOfType<Amulet>();
            foreach (FamiliarBase.Familiars f in Enum.GetValues(typeof(FamiliarBase.Familiars))) {
                var d = HeroMerchant.Instance.familiars.FindAll((x) => x.familiar == f).Count - GetFamiliarCount(f,a);
                for (int i = 0; i < d; i++)
                    FamiliarsManager.DespawnRequestedFamiliar(f, true);
                if (f == FamiliarBase.Familiars.WOOD_MIMIC)
                    continue;
                for (int i = 0; i < -d; i++)
                {
                    var nf = FamiliarsManager.AddFamiliar(f);
                    if (GameManager.Instance.IsWillInDungeon())
                    {
                        nf.OnChangeToDungeonArea();
                        nf.TeleportInFrontOfWill();
                    }
                }
                if (d < 0)
                    flag = true;
            }
            if (flag && ShopManager.Instance && !ShopManager.Instance.isWillInShop)
                foreach (var f in HeroMerchant.Instance.familiars)
                    if (f.CheckIfWillPositionIsWalkable())
                        f.TeleportInFrontOfWill();
        }
    }

    static class ExtentionMethods
    {
        public static bool Contains(this Array array, object value)
        {
            foreach (var v in array)
                if (v == value)
                    return true;
            return false;
        }
        public static void PrintAllFeilds<T>(this T value)
        {
            if (value == null)
                Debug.Log($"Type: {typeof(T).Namespace}.{typeof(T).Name} | Cannot read fields of NULL");
            string s = $"To String: {value}";
            var t = value.GetType();
            while (t != typeof(object))
            {
                s += $"\n --------- [{t.Namespace}.{t.Name}] Fields:";
                foreach (var f in t.GetFields((BindingFlags)(-1)))
                    if (!f.IsStatic)
                    {
                        var v = f.GetValue(value);
                        s += $"\n[{(string.IsNullOrEmpty(f.FieldType.Namespace) ? "" : (f.FieldType.Namespace + "."))}{f.FieldType.Name}] {f} = ";
                        if (v == null)
                            s += "{NULL}";
                        else if (f.FieldType.IsArray)
                            s += $"Size({(v as Array).Length})";
                        else if (v is ICollection)
                        {
                            try
                            {
                                s += $"Size({(v as ICollection).Count})";
                            }
                            catch
                            {
                                s += "Size(?)";
                            }
                        }
                        else
                            s += v.ToString();
                    }
                t = t.BaseType;
            }
            Debug.Log(s);
        }
    }

    public class SummonAmuletEffect : AmuletMoonlighterEffect
    {
        public virtual FamiliarBase.Familiars[] familiars { get; }
        protected override void ApplyToHeroMerchant(HeroMerchant merchant)
        {
            Main.AddFamiliars(this);
            OnEffectEnabled();
            base.ApplyToHeroMerchant(merchant);
        }
        protected override void RemoveFromHeroMerchant()
        {
            Main.RemoveFamiliars(this);
            OnEffectDisabled();
            base.RemoveFromHeroMerchant();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Main.RemoveFamiliars(this);
        }
    }

    public class SlimeAmuletEffect : SummonAmuletEffect
    {
        static FamiliarBase.Familiars[] _f;
        static SlimeAmuletEffect()
        {
            var f = new List<FamiliarBase.Familiars>();
            foreach (FamiliarBase.Familiars familiar in Enum.GetValues(typeof(FamiliarBase.Familiars)))
                if (familiar.ToString().Contains("BABY_SLIME"))
                    f.Add(familiar);
            _f = f.ToArray();
        }
        public override FamiliarBase.Familiars[] familiars => _f;
    }

    public class MotherAmuletEffect : SummonAmuletEffect
    {
        static FamiliarBase.Familiars[] _f = new FamiliarBase.Familiars[] {
            FamiliarBase.Familiars.DESERT_MOTHER_GOLEM,
            FamiliarBase.Familiars.DESERT_MOTHER_GOLEM,
            FamiliarBase.Familiars.DESERT_MOTHER_GOLEM
        };
        public override FamiliarBase.Familiars[] familiars => _f;
    }

    public class GolemDungeonAmuletEffect : SummonAmuletEffect
    {
        static FamiliarBase.Familiars[] _f = new FamiliarBase.Familiars[] {
            FamiliarBase.Familiars.GOLEM_FLYING_REPAIR,
            FamiliarBase.Familiars.GOLEM_TURRET
        };
        public override FamiliarBase.Familiars[] familiars => _f;
    }

    public class DesertDungeonAmuletEffect : SummonAmuletEffect
    {
        static FamiliarBase.Familiars[] _f = new FamiliarBase.Familiars[] {
            FamiliarBase.Familiars.DESERT_DANCER_PUPPET,
            FamiliarBase.Familiars.DESERT_MOTHER_GOLEM
        };
        public override FamiliarBase.Familiars[] familiars => _f;
    }

    public class ForestDungeonAmuletEffect : SummonAmuletEffect
    {
        static FamiliarBase.Familiars[] _f = new FamiliarBase.Familiars[] {
            FamiliarBase.Familiars.FOREST_BLADE_TREE,
            FamiliarBase.Familiars.FOREST_WIND_TREE
        };
        public override FamiliarBase.Familiars[] familiars => _f;
    }

    public class TeslaDungeonAmuletEffect : SummonAmuletEffect
    {
        static FamiliarBase.Familiars[] _f = new FamiliarBase.Familiars[] {
            FamiliarBase.Familiars.TESLA_GRAAF_GENERATOR,
            FamiliarBase.Familiars.TESLA_RECHARGER
        };
        public override FamiliarBase.Familiars[] familiars => _f;
    }

    public class MimicAmuletEffect : SummonAmuletEffect
    {
        public override FamiliarBase.Familiars[] familiars => new FamiliarBase.Familiars[0];
    }

    [HarmonyPatch(typeof(FamiliarsManager), "SpawnEquippedFamiliar")]
    static class Patch_FamiliarsPanel
    {
        static void Prefix(ref List<FamiliarBase> __state)
        {
            if (!HeroMerchant.Instance)
                return;
            __state = HeroMerchant.Instance.familiars;
            HeroMerchant.Instance.familiars = new List<FamiliarBase>();
        }
        static void Postfix(ref List<FamiliarBase> __state)
        {
            if (__state != null)
                HeroMerchant.Instance.familiars.AddRange(__state);
            Main.EnsureFamiliars();
        }

    }

    [HarmonyPatch(typeof(Weapon))]
    static class Patch_Weapon
    {
        [HarmonyPatch("AddSlimeFamiliarEffect")]
        [HarmonyPrefix]
        static bool AddSlimeFamiliarEffect(Weapon __instance, MoonlighterEffect effect) => __instance._slimeEffects == null || !__instance._slimeEffects.ContainsKey(effect);

        public static bool attacking = false;

        [HarmonyPatch("SendMainAttackHit")]
        [HarmonyPrefix]
        static void SendMainAttackHit_Pre() => attacking = true;

        [HarmonyPatch("SendMainAttackHit")]
        [HarmonyPostfix]
        static void SendMainAttackHit_Post() => attacking = false;

        [HarmonyPatch("SendSecondaryAttackHit")]
        [HarmonyPrefix]
        static void SendSecondaryAttackHit_Pre() => attacking = true;

        [HarmonyPatch("SendSecondaryAttackHit")]
        [HarmonyPostfix]
        static void SendSecondaryAttackHit_Post() => attacking = false;

        [HarmonyPatch("OnMainAttackHit")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnMainAttackHit(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            var code = instructions.ToList();
            var ind1 = -1;
            var ind2 = -1;
            var loc = iL.DeclareLocal(typeof(MoonlighterEffect),true);
            var lbl1 = iL.DefineLabel();
            var lbl2 = iL.DefineLabel();
            for (int i = 0; i < code.Count; i++) {
                if (code[i].opcode.FlowControl == FlowControl.Cond_Branch)
                {
                    ind1 = i + 1;
                    ind2 = -1;
                }
                else if (code[i].opcode == OpCodes.Call && code[i].operand is MethodInfo method)
                {
                    if (method.Name == "get_SlimeFamiliarEffect")
                        ind2 = i + 1;
                    else if (method.Name == "Create" && method.DeclaringType == typeof(MoonlighterEffect) && ind1 != -1 && ind2 != -1)
                    {
                        code.InsertRange(i + 2, new[] {
                            new CodeInstruction(OpCodes.Ldarg_0) { labels = new List<Label>() { lbl2 } },
                            new CodeInstruction(OpCodes.Ldloca_S,loc),
                            new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(PatchCalls),nameof(PatchCalls.GetNextEffect))),
                            new CodeInstruction(OpCodes.Brtrue,lbl1)

                        });
                        code.InsertRange(ind2, new[] {
                            new CodeInstruction(OpCodes.Ldloc,loc),
                            new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(PatchCalls),nameof(PatchCalls.Replace)).MakeGenericMethod(typeof(MoonlighterEffect)))
                            });
                        code[ind1].labels.Add(lbl1);
                        code.Insert(ind1, new CodeInstruction(OpCodes.Br,lbl2));
                        break;
                    }
                }
            }
            code.InsertRange(0, new[] {
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Stloc,loc)
                });
            /*var s = "";
            var n = 0;
            foreach (var i in code)
                s +=
                    n++ + ": "
                    + i.opcode + (
                        i.operand == null
                            ? ""
                            : i.operand is Label l
                                ? " " + code.FindIndex(x => x.labels.Contains(l))
                                : (" " + i.operand.ToString()))
                    + "\n";
            Debug.Log(s);*/
            return code;
        }
    }

    public static class PatchCalls
    {
        static Dictionary<MoonlighterEffect, MoonlighterEffect> order;
        public static bool GetNextEffect(Weapon weapon, ref MoonlighterEffect current)
        {
            if (current == null)
            {
                order = new Dictionary<MoonlighterEffect, MoonlighterEffect>();
                MoonlighterEffect last = null;
                foreach (var e in weapon._slimeEffects.Keys)
                    if (e)
                    {

                        if (current == null)
                            last = current = e;
                        else
                            last = order[last] = e;
                    }
                return current;
            }
            else
            {
                if (order.TryGetValue(current, out var next))
                    current = next;
                else
                    current = null;
                return current;
            }
        }
        public static T Replace<T>(T original, T replacement) => replacement;
    }

    [HarmonyPatch(typeof(BabySlimeFamiliarBehaviour), "RemoveEffectFromWeapons")]
    static class Patch_BabySlime
    {
        static bool Prefix(BabySlimeFamiliarBehaviour __instance, MoonlighterEffect effect) => HeroMerchant.Instance.familiars.FindAll((x) => x.familiar == __instance.familiar).Count <= 1;
    }

    [HarmonyPatch(typeof(Amulet), "HeroMerchantInventory_OnItemUnequipped")]
    static class Patch_Amulet_InventoryUnequipped
    {
        static bool Prefix(Amulet __instance, ItemStack arg1, HeroMerchantInventory.EquipmentSlot arg2) => arg1 && arg1.Amulet && arg1.Amulet == __instance;
    }

    [HarmonyPatch(typeof(Enemy), "DealDamageToEnemy")]
    static class Patch_DamageEnemy
    {
        static void Prefix(ref float attackStrength)
        {
            if (Patch_Weapon.attacking)
                attackStrength *= Main.GetFamiliarBoost;
        }
    }
    [HarmonyPatch(typeof(FamiliarBase), "AttackValue", MethodType.Getter)]
    static class Patch_GetFamiliarDamage
    {
        static void Postfix(FamiliarBase __instance, ref float __result) => __result += __result * (1 - Main.GetFamiliarBoost) / __instance.attackModifier;
    }
}