#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DemonFighter.Presentation.Combat
{
    /// <summary>
    /// Placeholder blood (GAME_DESIGN, "Visible damage and gore", stage 1): flat dark quads on the ground and small
    /// dark blobs stuck to bodies, reused oldest first so a long fight never allocates past the pool size. Body blood
    /// dries: a blob shrinks away over the last part of its life and is taken back (D-068); ground pools stay until
    /// the pool reuses them. Blood is look only; the simulation knows nothing about it.
    /// </summary>
    public sealed class BloodDecalPool
    {
        private const int GroundPoolSize = 160;
        private const int BodyPoolSize = 160;
        private const float GroundLift = 0.02f;
        private const float GroundSizeMin = 0.35f;
        private const float GroundSizeMax = 0.9f;
        private const float BodyBlobPerMeter = 0.12f;
        private const float GroundProbeHeight = 2f;
        private const float GroundProbeDepth = 6f;
        private const float BodyBlobSeconds = 15f;
        private const float BodyDryFraction = 0.3f;

        private readonly Material _blood;
        private readonly Transform _root;
        private readonly GameObject?[] _ground = new GameObject?[GroundPoolSize];
        private readonly GameObject?[] _body = new GameObject?[BodyPoolSize];
        private readonly float[] _bodyBorn = new float[BodyPoolSize];
        private readonly Vector3[] _bodyScale = new Vector3[BodyPoolSize];
        private int _nextGround;
        private int _nextBody;

        public BloodDecalPool(Material blood, Transform parent)
        {
            _blood = blood != null ? blood : throw new ArgumentNullException(nameof(blood));
            _root = new GameObject("Blood").transform;
            _root.SetParent(parent, false);
        }

        /// <summary>Puts a splash on the ground under a world position, sized by the bleeding body.</summary>
        public void SplashGround(Vector3 position, float sizeMeters)
        {
            int mask = ~(Layers.DemonMask | Layers.FoodMask);
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            Vector3 point = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeHeight + GroundProbeDepth, mask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
            GameObject quad = Take(_ground, ref _nextGround, PrimitiveType.Quad, out _);
            float size = sizeMeters * Random.Range(GroundSizeMin, GroundSizeMax);
            quad.transform.SetParent(_root, false);
            quad.transform.SetPositionAndRotation(point + Vector3.up * GroundLift, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            quad.transform.localScale = new Vector3(size, size, 1f);
        }

        /// <summary>Sticks a blob to a body at a world point; it follows the body from then on.</summary>
        public void SplashBody(Transform body, Vector3 point, float sizeMeters)
        {
            GameObject blob = Take(_body, ref _nextBody, PrimitiveType.Sphere, out int index);
            blob.transform.SetParent(body, true);
            blob.transform.position = point;
            Vector3 parentScale = body.lossyScale;
            float size = sizeMeters * BodyBlobPerMeter;
            var scale = new Vector3(size / parentScale.x, size / parentScale.y, size / parentScale.z);
            blob.transform.localScale = scale;
            _bodyBorn[index] = Time.time;
            _bodyScale[index] = scale;
        }

        /// <summary>Dries body blood: past most of its life a blob shrinks away, then it is taken back; call once per frame.</summary>
        public void Update(float now)
        {
            for (int i = 0; i < _body.Length; i++)
            {
                GameObject? blob = _body[i];
                if (blob == null || !blob.activeSelf)
                {
                    continue;
                }

                float age = now - _bodyBorn[i];
                if (age >= BodyBlobSeconds)
                {
                    blob.SetActive(false);
                    blob.transform.SetParent(_root, false);
                    continue;
                }

                float dry = Mathf.InverseLerp(BodyBlobSeconds * (1f - BodyDryFraction), BodyBlobSeconds, age);
                if (dry > 0f)
                {
                    blob.transform.localScale = _bodyScale[i] * (1f - dry);
                }
            }
        }

        /// <summary>Removes every decal; called when the run ends.</summary>
        public void Destroy()
        {
            for (int i = 0; i < _body.Length; i++)
            {
                if (_body[i] != null)
                {
                    Object.Destroy(_body[i]);
                }
            }

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        private GameObject Take(GameObject?[] pool, ref int next, PrimitiveType primitive, out int index)
        {
            index = next;
            GameObject? decal = pool[next];
            if (decal == null)
            {
                decal = GameObject.CreatePrimitive(primitive);
                decal.name = "Blood";
                Object.Destroy(decal.GetComponent<Collider>());
                decal.GetComponent<Renderer>().sharedMaterial = _blood;
                pool[next] = decal;
            }

            next = (next + 1) % pool.Length;
            decal.SetActive(true);
            return decal;
        }
    }
}
