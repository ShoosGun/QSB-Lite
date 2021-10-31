using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace ClientSide.Utils
{
    //TODO Refazer isso aki com uma lista no lugar de achar eles durante a "gameplay"
    public class MajorSectorLocator : MonoBehaviour
    {
        private static Dictionary<Sector.SectorName, MajorSector> majorSectors = new Dictionary<Sector.SectorName, MajorSector>();
        private void Awake()
        {
            foreach(var majorSector in (MajorSector[])FindObjectsOfType(typeof(MajorSector)))
                majorSectors.Add(majorSector.GetName(), majorSector);
        }
        private void OnDestroy()
        {
            majorSectors.Clear();
        }
        public static MajorSector GetMajorSector(Sector.SectorName sectorName)
        {
            majorSectors.TryGetValue(sectorName, out MajorSector sector);
            return sector;
        }
    }
}
