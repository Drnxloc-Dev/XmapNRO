﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Assembly_CSharp.Xmap
{
    public class XmapController : IActionListener
    {
        private const int TIME_DELAY_NEXTMAP = 200;
        private const int TIME_DELAY_RENEXTMAP = 500;
        private const int ID_ITEM_CAPSUAL_VIP = 194;
        private const int ID_ITEM_CAPSUAL = 193;

        public static bool IsXmapRunning;

        private static XmapController _Instance = new XmapController();
        private static int IdMapEnd;
        private static List<int> WayXmap;
        private static int IndexWay;
        private static bool IsNextMapFail;
        private static bool IsWait;
        private static long TimeStartWait;
        private static long TimeWait;
        private static bool IsWaitNextMap;

        public static void ShowXmapMenu()
        {
            MapConnection.LoadGroupMapsFromFile("TextData\\GroupMapsXmap.txt");

            MyVector myVector = new MyVector();
            foreach (var groupMap in MapConnection.GroupMaps)
                myVector.addElement(new Command(groupMap.NameGroup, _Instance, 1, groupMap.IdMaps));

            GameCanvas.menu.startAt(myVector, 3);
        }

        public static void ShowSelectMap(List<int> idMaps)
        {
            Pk9r.IsMapTransAsXmap = true;

            int len = idMaps.Count;
            GameCanvas.panel.mapNames = new string[len];
            GameCanvas.panel.planetNames = new string[len];
            for (int i = 0; i < len; i++)
            {
                string nameMap = TileMap.mapNames[idMaps[i]];
                GameCanvas.panel.mapNames[i] = idMaps[i] + ": " + nameMap;
                GameCanvas.panel.planetNames[i] = "Xmap by Phucprotein";
            }
            GameCanvas.panel.setTypeMapTrans();
            GameCanvas.panel.show();
        }

        public static void StartRunToMapId(int idMap)
        {
            IdMapEnd = idMap;
            IsXmapRunning = true;
        }

        public static void FinishXmap()
        {
            IsXmapRunning = false;
            IsNextMapFail = false;
            MapConnection.MyLinkMaps = null;
            WayXmap = null;
        }

        public static void UseCapsual()
        {
            Pk9r.IsShowPanelMapTrans = false;
            Service.gI().useItem(0, 1, -1, ID_ITEM_CAPSUAL);
            Service.gI().useItem(0, 1, -1, ID_ITEM_CAPSUAL_VIP);
        }

        public static void HideInfoDlg()
        {
            InfoDlg.hide();
        }

        public static void SaveIdMapCapsualReturn()
        {
            Pk9r.IdMapCapsualReturn = TileMap.mapID;
        }

        public static void Update()
        {
            if (IsWaiting())
                return;

            if (MapConnection.IsLoading)
                return;

            if (IsWaitNextMap)
            {
                Wait(TIME_DELAY_NEXTMAP);
                IsWaitNextMap = false;
                return;
            }

            if (IsNextMapFail)
            {
                MapConnection.MyLinkMaps = null;
                WayXmap = null;
                IsNextMapFail = false;
                return;
            }

            if (WayXmap == null)
            {
                if (!MapConnection.IsLoading && MapConnection.MyLinkMaps == null)
                {
                    MapConnection.LoadLinkMaps();
                    return;
                }
                WayXmap = Algorithm.FindWay(TileMap.mapID, IdMapEnd);
                IndexWay = 0;
                if (WayXmap == null)
                {
                    GameScr.info1.addInfo("Không thể tìm thấy đường đi", 0);
                    FinishXmap();
                    return;
                }
            }

            if ((TileMap.mapID == WayXmap[WayXmap.Count - 1]) && !Algorithm.IsMyCharDie())
            {
                GameScr.info1.addInfo("Xmap by Phucprotein", 0);
                FinishXmap();
                return;
            }

            if (TileMap.mapID == WayXmap[IndexWay])
            {
                if (Algorithm.IsMyCharDie())
                {
                    Service.gI().returnTownFromDead();
                    IsWaitNextMap = IsNextMapFail = true;                    
                }
                else if (Algorithm.CanNextMap())
                {
                    NextMap(WayXmap[IndexWay + 1]);
                    IsWaitNextMap = true;
                }
                Wait(TIME_DELAY_RENEXTMAP);
                return;
            }

            if (TileMap.mapID == WayXmap[IndexWay + 1])
            {
                IndexWay++;
                return;
            }

            IsNextMapFail = true;
        }

        private static void NextMap(int idMapNext)
        {
            List<MapNext> mapNexts = Algorithm.GetMapNexts(TileMap.mapID);
            if (mapNexts != null)
            {
                foreach (MapNext mapNext in mapNexts)
                {
                    if (mapNext.MapID == idMapNext)
                    {
                        NextMap(mapNext);
                        return;
                    }
                }
            }
            GameScr.info1.addInfo("Lỗi tại dữ liệu", 0);
        }

        private static void NextMap(MapNext mapNext)
        {
            switch (mapNext.Type)
            {
                case TypeMapNext.AutoWaypoint:
                    NextMapAutoWaypoint(mapNext);
                    break;

                case TypeMapNext.NpcMenu:
                    NextMapNpcMenu(mapNext);
                    break;

                case TypeMapNext.NpcPanel:
                    NextMapNpcPanel(mapNext);
                    break;

                case TypeMapNext.Position:
                    NextMapPosition(mapNext);
                    break;

                case TypeMapNext.Capsual:
                    NextMapCapsual(mapNext);
                    break;
            }
        }

        private static void NextMapAutoWaypoint(MapNext mapNext)
        {
            Waypoint waypoint = Algorithm.FindWaypoint(mapNext.MapID);
            if (waypoint != null)
                NextMapWaypoint(waypoint);
        }

        private static void NextMapNpcMenu(MapNext mapNext)
        {
            int idNpc = mapNext.Info[0];
            Service.gI().openMenu(idNpc);
            for (int i = 1; i < mapNext.Info.Length; i++)
            {
                int select = mapNext.Info[i];
                Service.gI().confirmMenu((short)idNpc, (sbyte)select);
            }
        }

        private static void NextMapNpcPanel(MapNext mapNext)
        {
            int idNpc = mapNext.Info[0];
            int selectMenu = mapNext.Info[1];
            int selectPanel = mapNext.Info[2];
            Service.gI().openMenu(idNpc);
            Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
            Service.gI().requestMapSelect(selectPanel);
        }

        private static void NextMapPosition(MapNext mapNext)
        {
            int xPos = mapNext.Info[0];
            int yPos = mapNext.Info[1];
            MoveMyChar(xPos, yPos);
            Service.gI().requestChangeMap();
        }

        private static void NextMapCapsual(MapNext mapNext)
        {
            SaveIdMapCapsualReturn();
            int index = mapNext.Info[0];
            Service.gI().requestMapSelect(index);
        }

        private static void NextMapWaypoint(Waypoint waypoint)
        {
            int x = Algorithm.GetPosWaypointX(waypoint);
            int y = Algorithm.GetPosWaypointY(waypoint);
            MoveMyChar(x, y);
            RequestChangeMap(waypoint);
        }

        private static void MoveMyChar(int x, int y)
        {
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();

            if (ItemTime.isExistItem(4387))
                return;

            Char.myCharz().cx = x;
            Char.myCharz().cy = y + 1;
            Service.gI().charMove();
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();
        }

        private static void RequestChangeMap(Waypoint waypoint)
        {
            if (waypoint.isOffline)
            {
                Service.gI().getMapOffline();
                return;
            }
            Service.gI().requestChangeMap();
        }

        private static void Wait(int time)
        {
            IsWait = true;
            TimeStartWait = mSystem.currentTimeMillis();
            TimeWait = time;
        }

        private static bool IsWaiting()
        {
            if (IsWait && (mSystem.currentTimeMillis() - TimeStartWait >= TimeWait))
                IsWait = false;
            return IsWait;
        }

        public void perform(int idAction, object p)
        {
            switch (idAction)
            {
                case 1:
                    List<int> idMaps = (List<int>)p;
                    ShowSelectMap(idMaps);
                    break;
            }
        }
    }
}
