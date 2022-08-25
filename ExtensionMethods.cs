using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.Gunleste {
    public static class ExtensionMethods {
        public static Player Holding(this BadelineBoost boost) => DynamicData.For(boost).Get<Player>("holding");
        public static void Holding(this BadelineBoost boost, Player value) => DynamicData.For(boost).Set("holding", value);
        
        
        public static bool Travelling(this BadelineBoost boost) => DynamicData.For(boost).Get<bool>("travelling");
        public static void Travelling(this BadelineBoost boost, bool value) => DynamicData.For(boost).Set("travelling", value);
        
        
        public static int NodeIndex(this BadelineBoost boost) => DynamicData.For(boost).Get<int>("nodeIndex");
        public static void NodeIndex(this BadelineBoost boost, int value) => DynamicData.For(boost).Set("nodeIndex", value);
        
        
        public static Sprite Sprite(this BadelineBoost boost) => DynamicData.For(boost).Get<Sprite>("sprite");
        public static void Sprite(this BadelineBoost boost, Sprite value) => DynamicData.For(boost).Set("sprite", value);
        
        
        public static Vector2[] Nodes(this BadelineBoost boost) => DynamicData.For(boost).Get<Vector2[]>("nodes");
        public static void Nodes(this BadelineBoost boost, Vector2[] value) => DynamicData.For(boost).Set("nodes", value);
        
        
        public static bool FinalCh9GoldenBoost(this BadelineBoost boost) => DynamicData.For(boost).Get<bool>("finalCh9GoldenBoost");
        public static void FinalCh9GoldenBoost(this BadelineBoost boost, bool value) => DynamicData.For(boost).Set("finalCh9GoldenBoost", value);
        
        
        public static bool FinalCh9Boost(this BadelineBoost boost) => DynamicData.For(boost).Get<bool>("finalCh9Boost");
        public static void FinalCh9Boost(this BadelineBoost boost, bool value) => DynamicData.For(boost).Set("finalCh9Boost", value);
        
        
        public static bool FinalCh9Dialog(this BadelineBoost boost) => DynamicData.For(boost).Get<bool>("finalCh9Dialog");
        public static void FinalCh9Dialog(this BadelineBoost boost, bool value) => DynamicData.For(boost).Set("finalCh9Dialog", value);
        
        
        public static Image Stretch(this BadelineBoost boost) => DynamicData.For(boost).Get<Image>("stretch");
        public static void Stretch(this BadelineBoost boost, Image value) => DynamicData.For(boost).Set("stretch", value);

        
        public static SoundSource RelocateSfx(this BadelineBoost boost) => DynamicData.For(boost).Get<SoundSource>("relocateSfx");
        public static void RelocateSfx(this BadelineBoost boost, SoundSource value) => DynamicData.For(boost).Set("relocateSfx", value);

        
        public static bool CanSkip(this BadelineBoost boost) => DynamicData.For(boost).Get<bool>("canSkip");
        public static void CanSkip(this BadelineBoost boost, bool value) => DynamicData.For(boost).Set("canSkip", value);
        
        public static VertexLight Light(this BadelineBoost boost) => DynamicData.For(boost).Get<VertexLight>("light");
        public static void Light(this BadelineBoost boost, VertexLight value) => DynamicData.For(boost).Set("light", value);

        public static BloomPoint Bloom(this BadelineBoost boost) => DynamicData.For(boost).Get<BloomPoint>("bloom");
        public static void Bloom(this BadelineBoost boost, BloomPoint value) => DynamicData.For(boost).Set("bloom", value);

        
        public static void Finish(this BadelineBoost boost) => DynamicData.For(boost).Invoke("Finish");
        
        public static void Skip(this BadelineBoost boost) => DynamicData.For(boost).Invoke("Skip");
    }
}