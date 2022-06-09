using Unity.Entities;
using UnityEngine;

namespace Alter.Characters
{

    [GenerateAuthoringComponent]
    public struct BulletHoleSpawner : IComponentData
    {
        public Entity entity;
    }

    public class BulletHolePrefab : MonoBehaviour, IConvertGameObjectToEntity
    {

        public static Entity prefabEntity;
        public GameObject prefabGameObject;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            using (BlobAssetStore blobAssetStore = new BlobAssetStore())
            {
                Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                    prefabGameObject,
                    GameObjectConversionSettings.FromWorld(dstManager.World, blobAssetStore)
                );
                BulletHolePrefab.prefabEntity = prefabEntity;
            }
        }
    }

    /*public partial class BulletHoleSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<BulletHoleSpawner>().ForEach(
                (ref BulletHoleSpawner bulletEntityComponent) =>
                {

                }
                ).WithoutBurst().Run();
        }
    }*/
}
