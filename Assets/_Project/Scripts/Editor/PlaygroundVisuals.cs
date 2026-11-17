using Listonosz.Gameplay;
using Listonosz.Grid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Listonosz.EditorTools
{
    /// <summary>
    /// Oprawa wizualna „cukierkowa" pod mobile: włącza URP, generuje paletę żywych materiałów URP/Lit,
    /// ubiera scenę Playground (zielona trawa, kolorowe domki ze spadzistym dachem, urząd, skrzynki,
    /// drzewa) oraz dorzuca post-processing (Bloom, podbite nasycenie/kontrast, winieta, tonemapping)
    /// i miękkie światło. Idempotentne — przy ponownym uruchomieniu czyści poprzednią scenerię.
    /// </summary>
    public static class PlaygroundVisuals
    {
        private const string GenDir = "Assets/_Project/Settings/Generated";
        private const string MatDir = "Assets/_Project/Settings/Generated/Materials";
        private const string ScenePath = "Assets/_Project/Scenes/Playground.unity";

        [MenuItem("Listonosz/Dowal grafikę")]
        public static void Dress()
        {
            EnsureRenderPipeline();

            // Cukierkowa paleta (żywe, lekko nasycone barwy).
            Material mGround = MakeMat("M_Ground", new Color(0.46f, 0.80f, 0.42f), 0.1f);
            Material mPostman = MakeMat("M_Postman", new Color(0.18f, 0.48f, 0.98f), 0.35f);
            Material mBag = MakeMat("M_Bag", new Color(1.00f, 0.78f, 0.18f), 0.35f);
            Material mOffice = MakeMat("M_Office", new Color(1.00f, 0.84f, 0.28f), 0.25f);
            Material mRoofRed = MakeMat("M_RoofRed", new Color(0.98f, 0.32f, 0.34f), 0.25f);
            Material mRoofBlue = MakeMat("M_RoofBlue", new Color(0.36f, 0.58f, 0.98f), 0.25f);
            Material mRoofTeal = MakeMat("M_RoofTeal", new Color(0.20f, 0.78f, 0.72f), 0.25f);
            Material[] houses =
            {
                MakeMat("M_House1", new Color(1.00f, 0.74f, 0.80f), 0.2f), // pastelowy róż
                MakeMat("M_House2", new Color(0.72f, 0.93f, 0.83f), 0.2f), // mięta
                MakeMat("M_House3", new Color(1.00f, 0.86f, 0.66f), 0.2f), // brzoskwinia
            };
            Material mTrunk = MakeMat("M_Trunk", new Color(0.52f, 0.34f, 0.20f), 0.1f);
            Material mLeaf = MakeMat("M_Leaf", new Color(0.36f, 0.82f, 0.42f), 0.15f);
            Material mLeaf2 = MakeMat("M_Leaf2", new Color(0.46f, 0.88f, 0.50f), 0.15f);
            Material mMailbox = MakeMat("M_Mailbox", new Color(0.96f, 0.26f, 0.30f), 0.4f);
            Material mRoad = MakeMat("M_Road", new Color(0.86f, 0.78f, 0.58f), 0.1f);
            Material mWater = MakeMat("M_Water", new Color(0.32f, 0.66f, 0.98f), 0.7f);
            Material mCloud = MakeMat("M_Cloud", new Color(1f, 1f, 1f), 0.05f);
            Material mBush = MakeMat("M_Bush", new Color(0.34f, 0.74f, 0.40f), 0.12f);
            Material mFlowerStem = MakeMat("M_FlowerStem", new Color(0.30f, 0.62f, 0.32f), 0.1f);
            Material[] flowerBlooms =
            {
                MakeMat("M_FlowerPink", new Color(1f, 0.45f, 0.66f), 0.3f),
                MakeMat("M_FlowerYellow", new Color(1f, 0.86f, 0.22f), 0.3f),
                MakeMat("M_FlowerWhite", new Color(1f, 0.98f, 0.95f), 0.3f),
                MakeMat("M_FlowerViolet", new Color(0.70f, 0.45f, 0.95f), 0.3f),
            };
            Material mLampPole = MakeMat("M_LampPole", new Color(0.28f, 0.30f, 0.34f), 0.3f);
            Material mLampGlow = MakeMat("M_LampGlow", new Color(1f, 0.92f, 0.6f), 0.5f);
            mLampGlow.EnableKeyword("_EMISSION");
            mLampGlow.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.4f) * 2.2f);
            EditorUtility.SetDirty(mLampGlow);
            Material mStone = MakeMat("M_Stone", new Color(0.82f, 0.80f, 0.76f), 0.2f);
            Material mWhite = MakeMat("M_White", new Color(1f, 0.98f, 0.92f), 0.25f);
            Material mDark = MakeMat("M_Dark", new Color(0.12f, 0.13f, 0.16f), 0.2f);
            Material mSignBlue = MakeMat("M_SignBlue", new Color(0.22f, 0.58f, 1f), 0.35f);
            Material mVanBody = MakeMat("M_VanBody", new Color(1f, 0.88f, 0.24f), 0.35f);
            Material mVanCab = MakeMat("M_VanCab", new Color(0.28f, 0.72f, 1f), 0.35f);
            Material[] mBalloons =
            {
                MakeMat("M_BalloonPink", new Color(1f, 0.35f, 0.68f), 0.55f),
                MakeMat("M_BalloonYellow", new Color(1f, 0.86f, 0.22f), 0.55f),
                MakeMat("M_BalloonBlue", new Color(0.28f, 0.64f, 1f), 0.55f),
                MakeMat("M_BalloonMint", new Color(0.38f, 0.95f, 0.68f), 0.55f),
            };
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // Idempotencja — usuń poprzednią scenerię/efekty.
            DestroyIfExists("Sceneria");
            DestroyIfExists("PostFX");
            GameObject existingPlayer = GameObject.Find("Player");
            if (existingPlayer != null)
            {
                Transform oldBag = existingPlayer.transform.Find("Torba");
                if (oldBag != null)
                {
                    Object.DestroyImmediate(oldBag.gameObject);
                }
            }

            var details = new GameObject("Sceneria");

            SetMat(GameObject.Find("Ground"), mGround);

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                SetMat(player, mPostman);
                AddChildBox(player.transform, "Torba", new Vector3(0f, 0.05f, -0.42f), new Vector3(0.55f, 0.5f, 0.35f), mBag);
            }

            GameObject office = GameObject.Find("PunktOdbioru_Urzad");
            if (office != null)
            {
                MakeBuilding(office, new Vector3(2.8f, 2.8f, 2.8f), mOffice, mRoofTeal, details.transform);
                Vector3 op = office.transform.position;
                AddBox(details.transform, "Szyld", new Vector3(op.x, 2.2f, op.z - 1.5f), new Vector3(2.0f, 0.55f, 0.14f), mMailbox);
            }

            Material[] roofs = { mRoofRed, mRoofBlue, mRoofTeal };
            for (int i = 0; i < PlaygroundSceneBuilder.Town.Length; i++)
            {
                string celName = "Cel_" + PlaygroundSceneBuilder.Town[i].Key;
                DecorateHouse(GameObject.Find(celName), houses[i % houses.Length], roofs[i % roofs.Length], mMailbox, details.transform);
            }

            var grid = AssetDatabase.LoadAssetAtPath<GridConfig>($"{GenDir}/GridConfig.asset");
            var gs = new GridSystem(grid);
            Vector2Int[] treeCells =
            {
                new Vector2Int(2, 9), new Vector2Int(9, 2), new Vector2Int(16, 4), new Vector2Int(5, 15),
                new Vector2Int(13, 16), new Vector2Int(17, 13), new Vector2Int(8, 8), new Vector2Int(3, 3),
                new Vector2Int(12, 6), new Vector2Int(6, 12), new Vector2Int(18, 9), new Vector2Int(10, 17),
            };
            for (int i = 0; i < treeCells.Length; i++)
            {
                Vector3 c = gs.CellToWorldCenter(treeCells[i]);
                AddTree(details.transform, new Vector3(c.x, 0f, c.z), mTrunk, (i % 2 == 0) ? mLeaf : mLeaf2);
            }

            // Siatka ulic (lekko nad podłożem — bez z-fightingu). Ulice E-W i N-S tworzą kratę miasteczka.
            float[] roadZ = { 9f, 17f, 25f, 33f };
            for (int i = 0; i < roadZ.Length; i++)
            {
                bool main = Mathf.Approximately(roadZ[i], 9f);
                AddRoadStrip(details.transform, new Vector3(21f, 0.03f, roadZ[i]), 36f, main ? 3.2f : 2.4f, mRoad);
            }

            float[] roadX = { 9f, 15f, 21f, 29f, 35f };
            for (int i = 0; i < roadX.Length; i++)
            {
                bool main = Mathf.Approximately(roadX[i], 21f);
                AddRoadStrip(details.transform, new Vector3(roadX[i], 0.03f, 21f), main ? 3.2f : 2.4f, 34f, mRoad);
            }

            // Latarnie wzdłuż głównych alei (świecące — łapią bloom).
            float[] lampX = { 6f, 14f, 22f, 30f, 38f };
            for (int i = 0; i < lampX.Length; i++)
            {
                AddStreetlamp(details.transform, new Vector3(lampX[i], 0f, 11f), mLampPole, mLampGlow);
            }

            float[] lampZ = { 6f, 14f, 30f, 38f };
            for (int i = 0; i < lampZ.Length; i++)
            {
                AddStreetlamp(details.transform, new Vector3(23f, 0f, lampZ[i]), mLampPole, mLampGlow);
            }

            // Fontanna na placu.
            AddFountain(details.transform, new Vector3(25f, 0f, 25f), mStone, mWater);

            // Miasteczkowe detale: przejscia, znaki, van i dekoracje.
            AddCrosswalk(details.transform, new Vector3(14f, 0.075f, 9f), true, mWhite);
            AddCrosswalk(details.transform, new Vector3(23f, 0.075f, 17f), false, mWhite);
            AddCrosswalk(details.transform, new Vector3(29f, 0.075f, 25f), true, mWhite);
            AddCrosswalk(details.transform, new Vector3(35f, 0.075f, 33f), false, mWhite);

            AddStreetSign(details.transform, new Vector3(7f, 0f, 8f), mLampPole, mSignBlue, mWhite);
            AddStreetSign(details.transform, new Vector3(22f, 0f, 18.5f), mLampPole, mSignBlue, mWhite);
            AddStreetSign(details.transform, new Vector3(32f, 0f, 32f), mLampPole, mSignBlue, mWhite);

            AddDeliveryVan(details.transform, new Vector3(13f, 0f, 10.8f), mVanBody, mVanCab, mDark, mWhite);
            AddBunting(details.transform, new Vector3(16f, 3.2f, 7.5f), new Vector3(24f, 3.2f, 7.5f), mBalloons);

            // Stawik.
            AddPond(details.transform, new Vector3(30f, 0.04f, 31f), 3.4f, mWater);

            // Krzaczki.
            Vector2Int[] bushCells =
            {
                new Vector2Int(3, 6), new Vector2Int(11, 3), new Vector2Int(8, 16), new Vector2Int(17, 16),
                new Vector2Int(15, 7), new Vector2Int(6, 9), new Vector2Int(19, 11),
            };
            for (int i = 0; i < bushCells.Length; i++)
            {
                Vector3 b = gs.CellToWorldCenter(bushCells[i]);
                AddBush(details.transform, new Vector3(b.x, 0f, b.z), mBush);
            }

            // Kwiatki (kępki).
            Vector2Int[] flowerCells =
            {
                new Vector2Int(9, 5), new Vector2Int(13, 10), new Vector2Int(6, 13),
                new Vector2Int(5, 4), new Vector2Int(12, 8), new Vector2Int(16, 12),
            };
            for (int i = 0; i < flowerCells.Length; i++)
            {
                Vector3 f = gs.CellToWorldCenter(flowerCells[i]);
                AddFlowerPatch(details.transform, new Vector3(f.x, 0f, f.z), mFlowerStem, flowerBlooms);
            }

            // Chmurki.
            AddCloud(details.transform, new Vector3(10f, 14f, 12f), mCloud);
            AddCloud(details.transform, new Vector3(26f, 15.5f, 8f), mCloud);
            AddCloud(details.transform, new Vector3(19f, 14.5f, 30f), mCloud);
            AddCloud(details.transform, new Vector3(33f, 15f, 24f), mCloud);
            AddCloud(details.transform, new Vector3(6f, 16f, 28f), mCloud);

            AddBalloons(details.transform, new Vector3(20f, 0f, 13.5f), mBalloons);
            AddBalloons(details.transform, new Vector3(29f, 0f, 21f), mBalloons);
            AddBalloons(details.transform, new Vector3(12f, 0f, 28f), mBalloons);

            SetupLighting();
            SetupPostProcessing(details.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Listonosz] Grafika dowalona (cukierkowo) — URP + post-processing + sceneria. Naciśnij Play.");
        }

        // ---------- Budynki / props ----------

        private static void DecorateHouse(GameObject go, Material body, Material roof, Material mailbox, Transform details)
        {
            if (go == null)
            {
                return;
            }

            MakeBuilding(go, new Vector3(1.8f, 2.0f, 1.8f), body, roof, details);
            Vector3 p = go.transform.position;
            AddBox(details, "Skrzynka_slupek", new Vector3(p.x + 0.9f, 0.4f, p.z - 1.25f), new Vector3(0.12f, 0.8f, 0.12f), mailbox);
            AddBox(details, "Skrzynka_box", new Vector3(p.x + 0.9f, 0.85f, p.z - 1.25f), new Vector3(0.35f, 0.28f, 0.5f), mailbox);
        }

        private static void MakeBuilding(GameObject body, Vector3 size, Material wall, Material roof, Transform details)
        {
            body.transform.localScale = size;
            Vector3 p = body.transform.position;
            p.y = size.y * 0.5f;
            body.transform.position = p;
            SetMat(body, wall);

            AddGableRoof(details, new Vector3(p.x, size.y, p.z), size.x, size.z, roof, body.name);
            AddBox(details, body.name + "_komin", new Vector3(p.x + size.x * 0.3f, size.y + 0.55f, p.z + size.z * 0.3f),
                new Vector3(0.25f, 0.7f, 0.25f), roof);
        }

        /// <summary>Spadzisty dach (gable) z dwóch pochylonych połaci — niezawodne (cube'y), kalenica wzdłuż X.</summary>
        private static void AddGableRoof(Transform parent, Vector3 topCenter, float sx, float sz, Material mat, string baseName)
        {
            float hd = sz * 0.55f;                       // pół-głębokość połaci (z lekkim okapem)
            float rh = Mathf.Max(0.6f, sz * 0.45f);      // wysokość kalenicy
            float slope = Mathf.Sqrt(hd * hd + rh * rh);
            float deg = Mathf.Atan2(rh, hd) * Mathf.Rad2Deg;
            float thick = 0.16f;
            float lenX = sx * 1.12f;

            GameObject left = AddBox(parent, baseName + "_dachL",
                new Vector3(topCenter.x, topCenter.y + rh * 0.5f, topCenter.z - hd * 0.5f),
                new Vector3(lenX, thick, slope), mat);
            left.transform.rotation = Quaternion.Euler(-deg, 0f, 0f);

            GameObject right = AddBox(parent, baseName + "_dachR",
                new Vector3(topCenter.x, topCenter.y + rh * 0.5f, topCenter.z + hd * 0.5f),
                new Vector3(lenX, thick, slope), mat);
            right.transform.rotation = Quaternion.Euler(deg, 0f, 0f);
        }

        private static void AddTree(Transform parent, Vector3 basePos, Material trunk, Material leaf)
        {
            GameObject trunkGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunkGo.name = "Drzewo_pien";
            trunkGo.transform.SetParent(parent, true);
            trunkGo.transform.position = new Vector3(basePos.x, 0.7f, basePos.z);
            trunkGo.transform.localScale = new Vector3(0.32f, 0.7f, 0.32f);
            StripCollider(trunkGo);
            SetMat(trunkGo, trunk);

            GameObject leafGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leafGo.name = "Drzewo_korona";
            leafGo.transform.SetParent(parent, true);
            leafGo.transform.position = new Vector3(basePos.x, 1.75f, basePos.z);
            leafGo.transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);
            StripCollider(leafGo);
            SetMat(leafGo, leaf);

            var bob = leafGo.AddComponent<Bobber>();
            bob.bobAmplitude = 0.06f;
            bob.bobSpeed = 1.1f;
            bob.swayDegrees = 2.5f;
        }

        private static void AddRoadStrip(Transform parent, Vector3 center, float lengthX, float lengthZ, Material mat)
        {
            AddBox(parent, "Droga", center, new Vector3(lengthX, 0.06f, lengthZ), mat);
        }

        private static void AddPond(Transform parent, Vector3 center, float radius, Material water)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Stawik";
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.transform.localScale = new Vector3(radius * 1.8f, 0.06f, radius * 1.8f);
            StripCollider(go);
            SetMat(go, water);
        }

        private static void AddStreetlamp(Transform parent, Vector3 basePos, Material pole, Material glow)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = "Latarnia_slup";
            p.transform.SetParent(parent, true);
            p.transform.position = new Vector3(basePos.x, 1.4f, basePos.z);
            p.transform.localScale = new Vector3(0.12f, 1.4f, 0.12f);
            StripCollider(p);
            SetMat(p, pole);

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Latarnia_zarowka";
            bulb.transform.SetParent(parent, true);
            bulb.transform.position = new Vector3(basePos.x, 2.9f, basePos.z);
            bulb.transform.localScale = Vector3.one * 0.5f;
            StripCollider(bulb);
            SetMat(bulb, glow);
        }

        private static void AddFountain(Transform parent, Vector3 center, Material stone, Material water)
        {
            GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "Fontanna_basen";
            basin.transform.SetParent(parent, true);
            basin.transform.position = new Vector3(center.x, 0.25f, center.z);
            basin.transform.localScale = new Vector3(3f, 0.25f, 3f);
            StripCollider(basin);
            SetMat(basin, stone);

            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w.name = "Fontanna_woda";
            w.transform.SetParent(parent, true);
            w.transform.position = new Vector3(center.x, 0.52f, center.z);
            w.transform.localScale = new Vector3(2.6f, 0.06f, 2.6f);
            StripCollider(w);
            SetMat(w, water);

            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.name = "Fontanna_kolumna";
            col.transform.SetParent(parent, true);
            col.transform.position = new Vector3(center.x, 0.9f, center.z);
            col.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            StripCollider(col);
            SetMat(col, stone);

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "Fontanna_miska";
            top.transform.SetParent(parent, true);
            top.transform.position = new Vector3(center.x, 1.4f, center.z);
            top.transform.localScale = new Vector3(1.3f, 0.12f, 1.3f);
            StripCollider(top);
            SetMat(top, water);
        }

        private static void AddCrosswalk(Transform parent, Vector3 center, bool acrossX, Material white)
        {
            for (int i = -2; i <= 2; i++)
            {
                Vector3 offset = acrossX ? new Vector3(i * 0.45f, 0f, 0f) : new Vector3(0f, 0f, i * 0.45f);
                Vector3 size = acrossX ? new Vector3(0.22f, 0.035f, 2.4f) : new Vector3(2.4f, 0.035f, 0.22f);
                AddBox(parent, "Przejscie_pasy", center + offset, size, white);
            }
        }

        private static void AddStreetSign(Transform parent, Vector3 basePos, Material pole, Material sign, Material icon)
        {
            AddBox(parent, "Znak_slup", new Vector3(basePos.x, 0.75f, basePos.z), new Vector3(0.10f, 1.5f, 0.10f), pole);
            AddBox(parent, "Znak_poczta", new Vector3(basePos.x, 1.75f, basePos.z), new Vector3(1.0f, 0.55f, 0.12f), sign);
            AddBox(parent, "Znak_koperta", new Vector3(basePos.x, 1.75f, basePos.z - 0.07f), new Vector3(0.54f, 0.24f, 0.04f), icon);
            AddBox(parent, "Znak_koperta_skrzydlo", new Vector3(basePos.x - 0.16f, 1.75f, basePos.z - 0.10f), new Vector3(0.25f, 0.04f, 0.05f), icon)
                .transform.rotation = Quaternion.Euler(0f, 0f, 28f);
            AddBox(parent, "Znak_koperta_skrzydlo", new Vector3(basePos.x + 0.16f, 1.75f, basePos.z - 0.10f), new Vector3(0.25f, 0.04f, 0.05f), icon)
                .transform.rotation = Quaternion.Euler(0f, 0f, -28f);
        }

        private static void AddDeliveryVan(Transform parent, Vector3 basePos, Material body, Material cab, Material wheel, Material window)
        {
            AddBox(parent, "Van_poczta_paka", new Vector3(basePos.x, 0.75f, basePos.z), new Vector3(2.2f, 1.1f, 1.25f), body);
            AddBox(parent, "Van_poczta_kabina", new Vector3(basePos.x + 1.45f, 0.65f, basePos.z), new Vector3(0.9f, 0.9f, 1.18f), cab);
            AddBox(parent, "Van_poczta_szyba", new Vector3(basePos.x + 1.92f, 0.95f, basePos.z - 0.02f), new Vector3(0.08f, 0.45f, 0.78f), window);
            AddBox(parent, "Van_poczta_koperta", new Vector3(basePos.x - 0.25f, 0.9f, basePos.z - 0.66f), new Vector3(0.85f, 0.38f, 0.04f), window);

            Vector3[] wheels =
            {
                new Vector3(basePos.x - 0.75f, 0.22f, basePos.z - 0.68f),
                new Vector3(basePos.x + 1.05f, 0.22f, basePos.z - 0.68f),
                new Vector3(basePos.x - 0.75f, 0.22f, basePos.z + 0.68f),
                new Vector3(basePos.x + 1.05f, 0.22f, basePos.z + 0.68f),
            };
            for (int i = 0; i < wheels.Length; i++)
            {
                GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = "Van_poczta_kolo";
                w.transform.SetParent(parent, true);
                w.transform.position = wheels[i];
                w.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                w.transform.localScale = new Vector3(0.32f, 0.12f, 0.32f);
                StripCollider(w);
                SetMat(w, wheel);
            }
        }

        private static void AddBalloons(Transform parent, Vector3 basePos, Material[] mats)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 p = basePos + new Vector3(Mathf.Cos(angle) * 0.28f, 2.2f + i * 0.18f, Mathf.Sin(angle) * 0.28f);
                AddBox(parent, "Balon_sznurek", new Vector3((basePos.x + p.x) * 0.5f, 1.25f, (basePos.z + p.z) * 0.5f),
                    new Vector3(0.035f, 1.8f, 0.035f), mats[(i + 1) % mats.Length]);

                GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                b.name = "Balon";
                b.transform.SetParent(parent, true);
                b.transform.position = p;
                b.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);
                StripCollider(b);
                SetMat(b, mats[i % mats.Length]);

                var bob = b.AddComponent<Bobber>();
                bob.bobAmplitude = 0.08f;
                bob.bobSpeed = 1.6f + i * 0.2f;
                bob.driftAmplitude = 0.08f;
                bob.driftSpeed = 0.9f;
            }
        }

        private static void AddBunting(Transform parent, Vector3 from, Vector3 to, Material[] mats)
        {
            int count = 12;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y -= Mathf.Sin(t * Mathf.PI) * 0.35f;
                AddBox(parent, "Choragiewka", p, new Vector3(0.32f, 0.46f, 0.05f), mats[i % mats.Length]);
            }
        }

        private static void AddBush(Transform parent, Vector3 basePos, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Krzak";
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(basePos.x, 0.35f, basePos.z);
            go.transform.localScale = new Vector3(1.2f, 0.75f, 1.2f);
            StripCollider(go);
            SetMat(go, mat);
        }

        private static void AddFlowerPatch(Transform parent, Vector3 basePos, Material stem, Material[] blooms)
        {
            Vector2[] offsets =
            {
                new Vector2(0f, 0f), new Vector2(0.35f, 0.2f), new Vector2(-0.3f, 0.25f), new Vector2(0.1f, -0.35f),
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 p = new Vector3(basePos.x + offsets[i].x, 0f, basePos.z + offsets[i].y);

                GameObject st = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                st.name = "Kwiat_lodyga";
                st.transform.SetParent(parent, true);
                st.transform.position = new Vector3(p.x, 0.22f, p.z);
                st.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
                StripCollider(st);
                SetMat(st, stem);

                GameObject bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bloom.name = "Kwiat";
                bloom.transform.SetParent(parent, true);
                bloom.transform.position = new Vector3(p.x, 0.5f, p.z);
                bloom.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                StripCollider(bloom);
                SetMat(bloom, blooms[i % blooms.Length]);
            }
        }

        private static void AddCloud(Transform parent, Vector3 pos, Material mat)
        {
            var cloud = new GameObject("Chmurka");
            cloud.transform.SetParent(parent, true);
            cloud.transform.position = pos;

            Vector3[] puffs =
            {
                new Vector3(0f, 0f, 0f), new Vector3(1.6f, -0.2f, 0.2f), new Vector3(-1.5f, -0.1f, -0.2f),
                new Vector3(0.7f, 0.4f, 0.4f), new Vector3(-0.7f, 0.3f, -0.3f),
            };
            float[] sizes = { 2.6f, 2.0f, 2.1f, 1.7f, 1.6f };
            for (int i = 0; i < puffs.Length; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Puff";
                puff.transform.SetParent(cloud.transform, false);
                puff.transform.localPosition = puffs[i];
                puff.transform.localScale = Vector3.one * sizes[i];
                StripCollider(puff);
                SetMat(puff, mat);
            }

            var bob = cloud.AddComponent<Bobber>();
            bob.bobAmplitude = 0.4f;
            bob.bobSpeed = 0.5f;
            bob.driftAmplitude = 1.4f;
            bob.driftSpeed = 0.22f;
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 worldPos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.localScale = size;
            StripCollider(go);
            SetMat(go, mat);
            return go;
        }

        private static void AddChildBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            StripCollider(go);
            SetMat(go, mat);
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
        }

        private static void SetMat(GameObject go, Material mat)
        {
            if (go == null)
            {
                return;
            }

            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
            }
        }

        private static void DestroyIfExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---------- Światło + Post-processing ----------

        private static void SetupLighting()
        {
            GameObject lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                lightGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
                Light l = lightGo.GetComponent<Light>();
                if (l != null)
                {
                    l.color = new Color(1f, 0.97f, 0.88f);
                    l.intensity = 1.25f;
                    l.shadows = LightShadows.Soft;
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.70f, 0.82f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.70f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.38f, 0.34f);
        }

        private static void SetupPostProcessing(Transform parent)
        {
            string profilePath = $"{GenDir}/PostFX_Profile.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
            {
                AssetDatabase.DeleteAsset(profilePath);
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.95f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.65f);

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(30f);
            color.contrast.Override(10f);
            color.postExposure.Override(0.2f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.45f);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);

            AssetDatabase.SaveAssets();

            var pfx = new GameObject("PostFX");
            pfx.transform.SetParent(parent, false);
            Volume vol = pfx.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = profile;

            Camera cam = Camera.main;
            if (cam != null)
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = true;
                    data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                }
            }
        }

        // ---------- Materiały / URP ----------

        private static Material MakeMat(string name, Color color, float smoothness = 0.1f)
        {
            EnsureFolder(MatDir);
            string path = $"{MatDir}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureRenderPipeline()
        {
            string rendererPath = $"{GenDir}/UniversalRenderer.asset";
            string urpPath = $"{GenDir}/URP_Asset.asset";

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
            if (urp == null)
            {
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (rd == null)
                {
                    rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rd, rendererPath);
                }

                urp = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(urp, urpPath);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
