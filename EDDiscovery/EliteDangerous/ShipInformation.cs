﻿/*
 * Copyright © 2016 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EDDiscovery.EliteDangerous.JournalEvents;
using Newtonsoft.Json.Linq;

namespace EDDiscovery.EliteDangerous
{
    [System.Diagnostics.DebuggerDisplay("{Ship} {Modules.Count}")]
    public class ShipInformation
    {
        public int ID { get; private set; }                 // its ID.     ID's are moved to high range when sold
        public bool Sold { get; set; }                      // if sold.
        public string ShipType { get; private set; }        // ship type name, nice, fer-de-lance, etc.           can be null
        public string ShipFD { get; private set; }          // ship type name, fdname
        public string ShipUserName { get; private set; }    // ship name, may be empty or null
        public string ShipUserIdent { get; private set; }   // ship ident, may be empty or null

        public enum SubVehicleType
        {
            None, SRV, Fighter
        }

        public SubVehicleType SubVehicle { get; private set; } = SubVehicleType.None;    // if in a sub vehicle or mothership

        public Dictionary<string, JournalLoadout.ShipModule> Modules { get; private set; }

        public string ShipFullInfo
        {
            get                  // unique ID
            {
                StringBuilder sb = new StringBuilder(64);
                if ( ShipUserIdent!=null)
                    sb.Append(ShipUserIdent);
                sb.AppendPrePad(ShipUserName);
                sb.AppendPrePad(ShipType);
                sb.AppendPrePad("(" + ID.ToString() + ")");

                if (SubVehicle == SubVehicleType.SRV)
                    sb.AppendPrePad(" in SRV");
                else if (SubVehicle == SubVehicleType.Fighter)
                    sb.AppendPrePad(" Control Fighter");
                else
                {
                    int cap = FuelCapacity();
                    if (cap > 0)
                        sb.Append(" Fuel Cap " + cap);
                    cap = CargoCapacity();
                    if (cap > 0)
                        sb.Append(" Cargo Cap " + cap);
                }

                return sb.ToString();
            }
        }

        public string Name          // Name of ship, either user named or ship type
        {
            get                  // unique ID
            {
                if (ShipUserName != null && ShipUserName.Length > 0)
                    return ShipUserName;
                else
                    return ShipType;
            }
        }

        public string ShipShortName
        {
            get                  // unique ID
            {
                StringBuilder sb = new StringBuilder(64);
                if (ShipUserName != null && ShipUserName.Length > 0)
                {
                    sb.AppendPrePad(ShipUserName);
                }
                else
                {
                    sb.AppendPrePad(ShipType);
                    sb.AppendPrePad("(" + ID.ToString() + ")");
                }
                return sb.ToString();
            }
        }

        public int FuelCapacity()
        {
            int cap = 0;
            foreach(JournalLoadout.ShipModule sm in Modules.Values )
            {
                int classpos;
                if ( sm.Item.Contains("Fuel Tank") && (classpos = sm.Item.IndexOf("Class "))!=-1)
                {
                    char digit = sm.Item[classpos + 6];
                    cap += (1 << (digit - '0'));        // 1<<1 = 2.. 1<<2 = 4, etc.
                }
            }

            return cap;
        }

        public int CargoCapacity()
        {
            int cap = 0;
            foreach (JournalLoadout.ShipModule sm in Modules.Values)
            {
                int classpos;
                if (sm.Item.Contains("Cargo Rack") && (classpos = sm.Item.IndexOf("Class ")) != -1)
                {
                    char digit = sm.Item[classpos + 6];
                    cap += (1 << (digit - '0'));        // 1<<1 = 2.. 1<<2 = 4, etc.
                }
            }

            return cap;
        }

        public ShipInformation(int id)
        {
            ID = id;
            Modules = new Dictionary<string, JournalLoadout.ShipModule>();
        }

        public ShipInformation ShallowClone()          // shallow clone.. does not clone the ship modules, just the dictionary
        {
            ShipInformation sm = new ShipInformation(this.ID);
            sm.Sold = this.Sold;
            sm.ShipType = this.ShipType;
            sm.ShipFD = this.ShipFD;
            sm.ShipUserName = this.ShipUserName;
            sm.ShipUserIdent = this.ShipUserIdent;
            sm.SubVehicle = this.SubVehicle;
            sm.Modules = new Dictionary<string, JournalLoadout.ShipModule>(this.Modules);
            return sm;
        }

        public bool Contains(string slot)
        {
            return Modules.ContainsKey(slot);
        }

        public bool Same(JournalLoadout.ShipModule sm)
        {
            if (Modules.ContainsKey(sm.Slot))
            {
                bool v = Modules[sm.Slot].Same(sm);
                if (v)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public void Set(JournalLoadout.ShipModule sm)
        {
            if ( Modules.ContainsKey(sm.Slot) )
            {
                JournalLoadout.ShipModule oldsm = Modules[sm.Slot];

                if (sm.Item.Equals(oldsm.Item) && sm.LocalisedItem == null && oldsm.LocalisedItem != null)  // if item the same, old one has a localised name..
                    sm.LocalisedItem = oldsm.LocalisedItem;

            }
            Modules[sm.Slot] = sm;
        }

        public ShipInformation Set(string ship, string shipfd, string name = null, string ident = null)
        {
            if ( shipfd == null )
                System.Diagnostics.Debug.WriteLine("No FD Name!");

            if (ship != ShipType || (name != null && name != ShipUserName) || 
                                (ident != null && ident != ShipUserIdent) )
            {
                ShipInformation sm = this.ShallowClone();

                sm.ShipType = ship;
                sm.ShipFD = shipfd;
                if (name != null)
                    sm.ShipUserName = name;
                if (ident != null)
                    sm.ShipUserIdent = ident;

                return sm;
            }

            return this;
        }

        public ShipInformation SetSubVehicle(SubVehicleType vh)
        {
            if (vh != this.SubVehicle)
            {
                ShipInformation sm = this.ShallowClone();
                sm.SubVehicle = vh;
                return sm;
            }
            else
                return this;
        }

        public ShipInformation AddModule(string slot, string slotfd, string item, string itemfd, string itemlocalised)
        {
            if (!Modules.ContainsKey(slot) || !Modules[slot].Same(item))       // if does not have it, or item is not the same..
            {
                ShipInformation sm = this.ShallowClone();
                sm.Modules[slot] = new JournalLoadout.ShipModule(slot, slotfd, item, itemfd, itemlocalised);
                return sm;
            }
            return this;
        }

        public ShipInformation RemoveModule(string slot)
        {
            if (Modules.ContainsKey(slot))       // if has it..
            {
                ShipInformation sm = this.ShallowClone();
                sm.Modules.Remove(slot);
                return sm;
            }
            return this;
        }

        public ShipInformation RemoveModules(ModuleItem[] items)
        {
            ShipInformation sm = null;
            foreach (ModuleItem it in items)
            {
                if (Modules.ContainsKey(it.Slot))       // if has it..
                {
                    if (sm == null)
                        sm = this.ShallowClone();

                    sm.Modules.Remove(it.Slot);
                }
            }

            return sm ?? this;
        }

        public ShipInformation SwapModule(string fromslot, string fromslotfd, string fromitem , string fromitemfd, string fromiteml, 
                                          string toslot , string toslotfd, string toitem, string toitemfd, string toiteml ) 
        {
            ShipInformation sm = this.ShallowClone();
            if ( Modules.ContainsKey(fromslot))
            {
                if (Modules.ContainsKey(toslot))
                {
                    sm.Modules[fromslot] = new JournalLoadout.ShipModule(fromslot, fromslotfd, toitem, toitemfd, toiteml);
                }
                else
                    sm.Modules.Remove(fromslot);

                sm.Modules[toslot] = new JournalLoadout.ShipModule(toslot, toslotfd, fromitem, fromitemfd, fromiteml);
            }
            return sm;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.AppendFormat("Ship {0}", ShipFullInfo);
            sb.Append(Environment.NewLine);
            foreach (JournalLoadout.ShipModule sm in Modules.Values)
            {
                sb.AppendFormat(sm.ToString());
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public bool CheckMinimumJSONModules()
        {
            // these are required slots..
            string[] requiredmodules = { "PowerPlant", "MainEngines", "FrameShiftDrive", "LifeSupport", "PowerDistributor", "Radar", "FuelTank", "Armour" };
            int reqmodules = 0;

            foreach (JournalLoadout.ShipModule sm in Modules.Values)
            {
                int index = Array.FindIndex(requiredmodules, x => x.Equals(sm.SlotFD));
                if (index >= 0)
                    reqmodules |= (1 << index);     // bit map them in, the old fashioned way
            }

            return (reqmodules == (1 << requiredmodules.Length) - 1);
        }

        public string ToJSON(out string errstring)          
        {
            JObject jo = new JObject();

            jo["name"] = ShipFD;

            JObject mlist = new JObject();

            errstring = "";

            foreach (JournalLoadout.ShipModule sm in Modules.Values)
            {
                JObject module = new JObject();

                int edid = ModuleEDID.Instance.CalcID(sm.ItemFD,ShipFD);

                if (edid == 0)
                    errstring += sm.Item + ":" + sm.ItemFD + Environment.NewLine;

                if (edid > 0)
                    module["id"] = edid;

                module["name"] = sm.ItemFD;
                module["on"] = sm.Enabled.HasValue ? sm.Enabled : true;
                module["priority"] = sm.Priority.HasValue ? sm.Priority : 1;

                JObject minfo = new JObject();
                minfo["module"] = module;

                mlist[sm.SlotFD] = minfo;
            }

            jo["modules"] = mlist;

            return jo.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }

    public class ModulesInStore
    {
        public List<JournalLoadout.ShipModule> StoredModules { get; private set; }       // by STORE id

        public ModulesInStore()
        {
            StoredModules = new List<JournalLoadout.ShipModule>();
        }

        public ModulesInStore( List<JournalLoadout.ShipModule> list )
        {
            StoredModules = new List<JournalLoadout.ShipModule>(list);
        }

        public ModulesInStore StoreModule(string item, string itemlocalised)
        {
            ModulesInStore mis = this.ShallowClone();
            mis.StoredModules.Add(new JournalLoadout.ShipModule("", "", item, "", itemlocalised));
            return mis;
        }

        public ModulesInStore StoreModule(ModuleItem[] items, Dictionary<string, string> itemlocalisation)
        {
            ModulesInStore mis = this.ShallowClone();
            foreach (ModuleItem it in items)
            {
                string local = itemlocalisation.ContainsKey(it.Name) ? itemlocalisation[it.Name] : "";
                mis.StoredModules.Add(new JournalLoadout.ShipModule("", "", it.Name, "", local));
            }
            return mis;
        }

        public ModulesInStore RemoveModule(string item)
        {
            int index = StoredModules.FindIndex(x => x.Item.Equals(item, StringComparison.InvariantCultureIgnoreCase));  // if we have an item of this name
            if (index != -1)
            {
                ModulesInStore mis = this.ShallowClone();
                mis.StoredModules.RemoveAt(index);
                return mis;
            }
            else
                return this;
        }

        public ModulesInStore ShallowClone()          // shallow clone.. does not clone the ship modules, just the dictionary
        {
            ModulesInStore mis = new ModulesInStore(this.StoredModules);
            return mis;
        }
    }



    [System.Diagnostics.DebuggerDisplay("{currentid} ships {Ships.Count}")]
    public class ShipInformationList
    {
        public Dictionary<int, ShipInformation> Ships { get; private set; }         // by shipid

        public ModulesInStore StoredModules { get; private set; }                   // stored modules

        private Dictionary<string, string> itemlocalisation;

        private int currentid;
        public bool HaveCurrentShip { get { return currentid >= 0; } }
        public ShipInformation CurrentShip { get { return (HaveCurrentShip) ? Ships[currentid] : null; } }

        public ShipInformation GetShipByShortName(string sn)
        {
            List<ShipInformation> lst = Ships.Values.ToList();
            int index = lst.FindIndex(x => x.ShipShortName.Equals(sn));
            return (index >= 0) ? lst[index] : null;
        }

        public ShipInformation GetShipByFullInfoMatch(string sn)
        {
            List<ShipInformation> lst = Ships.Values.ToList();
            int index = lst.FindIndex(x => x.ShipFullInfo.IndexOf(sn, StringComparison.InvariantCultureIgnoreCase)!=-1);
            return (index >= 0) ? lst[index] : null;
        }

        private int newsoldid = 30000;

        public ShipInformationList()
        {
            Ships = new Dictionary<int, ShipInformation>();
            StoredModules = new ModulesInStore();
            itemlocalisation = new Dictionary<string, string>();
            currentid = -1;
        }

        public void Loadout(int id, string ship, string shipfd, string name, string ident, List<JournalLoadout.ShipModule> modulelist)
        {
            ShipInformation sm = EnsureShip(id);            // this either gets current ship or makes a new one.
            sm.Set(ship, shipfd, name, ident);
            //System.Diagnostics.Debug.WriteLine("Loadout " + sm.ID + " " + sm.Ship);

            ShipInformation newsm = null;

            foreach (JournalLoadout.ShipModule m in modulelist)
            {
                if (!sm.Contains(m.Slot) || !sm.Same(m))  // no slot, or not the same data.. (ignore localised item)
                {
                    if (m.LocalisedItem == null && itemlocalisation.ContainsKey(m.Item))        // if we have a cached localisation, use it
                        m.LocalisedItem = itemlocalisation[m.Item];

                    if (newsm == null)              // if not cloned
                    {
                        newsm = sm.ShallowClone();  // we need a clone, pointing to the same modules, but with a new dictionary
                        Ships[id] = newsm;              // update our record of last module list for this ship
                    }

                    newsm.Set(m);                   // update entry only.. rest will still point to same entries
                }
            }
        }

        public void LoadGame(int id, string ship, string shipfd, string name, string ident)        // LoadGame..
        {
            ShipInformation sm = EnsureShip(id);            // this either gets current ship or makes a new one.

            Ships[id] = sm = sm.Set(ship, shipfd, name, ident);   // this makes a shallow copy if any data has changed..

            //System.Diagnostics.Debug.WriteLine("Load Game " + sm.ID + " " + sm.Ship);

            if (!JournalFieldNaming.IsSRVOrFighter(ship))
                currentid = sm.ID;
        }

        public void LaunchSRV()
        {
            //System.Diagnostics.Debug.WriteLine("Launch SRV");
            if (HaveCurrentShip)
                Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.SRV);
        }

        public void DockSRV()
        {
            //System.Diagnostics.Debug.WriteLine("Dock SRV");
            if (HaveCurrentShip)
                Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.None);
        }

        public void LaunchFighter(bool pc)
        {
            //System.Diagnostics.Debug.WriteLine("Launch Fighter");
            if (HaveCurrentShip && pc==true)
                Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.Fighter);
        }

        public void DockFighter()
        {
            //System.Diagnostics.Debug.WriteLine("Dock Fighter");
            if (HaveCurrentShip)
                Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.None);
        }

        public void Resurrect()       
        {
            if (HaveCurrentShip)           // resurrect always in ship
            {
                Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.None);
            }
        }

        public void VehicleSwitch(string to)
        {
            if (HaveCurrentShip)
            {
                if (to == "Fighter")
                    Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.Fighter);
                else
                    Ships[currentid] = Ships[currentid].SetSubVehicle(ShipInformation.SubVehicleType.None);
            }
        }

        public void ShipyardSwap(JournalShipyardSwap e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.
            Ships[e.ShipId] = sm.Set(e.ShipType, e.ShipFD); // shallow copy if changed
            currentid = e.ShipId;
        }

        public void ShipyardNew(JournalShipyardNew e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.
            Ships[e.ShipId] = sm.Set(e.ShipType,e.ShipFD); // shallow copy if changed
            currentid = e.ShipId;
        }

        public void SetUserShipName(JournalSetUserShipName e)
        {
            ShipInformation sm = EnsureShip(e.ShipID);            // this either gets current ship or makes a new one.
            Ships[e.ShipID] = sm.Set(e.Ship, e.ShipFD, e.ShipName, e.ShipIdent);
            currentid = e.ShipID;           // must be in it to do this
        }

        public void ModuleBuy(JournalModuleBuy e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);              // this either gets current ship or makes a new one.

            if ( e.StoredItem.Length>0)                             // if we stored something
                StoredModules = StoredModules.StoreModule(e.StoredItem, e.StoredItemLocalised);

                                                                    // if we sold it, who cares?
            Ships[e.ShipId] = sm.AddModule(e.Slot,e.SlotFD, e.BuyItem,e.BuyItemFD, e.BuyItemLocalised);      // replace the slot with this

            itemlocalisation[e.BuyItem] = e.BuyItemLocalised;       // record any localisations
            if (e.SellItem.Length>0)
                itemlocalisation[e.SellItem] = e.SellItemLocalised;
            if (e.StoredItem.Length>0)
                itemlocalisation[e.StoredItem] = e.StoredItemLocalised;

            currentid = e.ShipId;           // must be in it to do this
        }

        public void ModuleSell(JournalModuleSell e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.
            Ships[e.ShipId] = sm.RemoveModule(e.Slot);

            if (e.SellItem.Length>0)
                itemlocalisation[e.SellItem] = e.SellItemLocalised;

            currentid = e.ShipId;           // must be in it to do this
        }

        public void ModuleSwap(JournalModuleSwap e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.
            Ships[e.ShipId] = sm.SwapModule(e.FromSlot, e.FromSlotFD, e.FromItem, e.FromItemFD, e.FromItemLocalised, 
                                            e.ToSlot , e.ToSlotFD, e.ToItem , e.ToItemFD, e.ToItemLocalised);
            currentid = e.ShipId;           // must be in it to do this
        }

        public void ModuleStore(JournalModuleStore e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.

            if (e.ReplacementItem.Length > 0)
                Ships[e.ShipId] = sm.AddModule(e.Slot, e.SlotFD, e.ReplacementItem, e.ReplacementItemFD, e.ReplacementItemLocalised);
            else
                Ships[e.ShipId] = sm.RemoveModule(e.Slot);

            StoredModules = StoredModules.StoreModule(e.StoredItem, e.StoredItemLocalised);
            currentid = e.ShipId;           // must be in it to do this
        }

        public void ModuleRetrieve(JournalModuleRetrieve e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.

            if ( e.SwapOutItem.Length>0 )
                StoredModules = StoredModules.StoreModule(e.SwapOutItem, e.SwapOutItemLocalised);

            Ships[e.ShipId] = sm.AddModule(e.Slot, e.SlotFD, e.RetrievedItem, e.RetrievedItemFD, e.RetrievedItemLocalised);

            StoredModules = StoredModules.RemoveModule(e.RetrievedItem);
        }

        public void ModuleSellRemote(JournalModuleSellRemote e)
        {
            StoredModules = StoredModules.RemoveModule(e.SellItem);
        }

        public void MassModuleStore(JournalMassModuleStore e)
        {
            ShipInformation sm = EnsureShip(e.ShipId);            // this either gets current ship or makes a new one.
            Ships[e.ShipId] = sm.RemoveModules(e.ModuleItems);
            StoredModules = StoredModules.StoreModule(e.ModuleItems, itemlocalisation);
        }

        #region Helpers

        private ShipInformation EnsureShip(int id)      // ensure we have an ID of this type..
        {
            if (Ships.ContainsKey(id))
            {
                ShipInformation sm = Ships[id];
                if (!sm.Sold)               // if not sold, ok
                    return sm;             
                else
                {
                    Ships[newsoldid++] = sm;                      // okay, we place this information on 30000+  all Ids of this will now refer to new entry
                }
            }

            ShipInformation smn = new ShipInformation(id);
            Ships[id] = smn;
            return smn;
        }

        #endregion

        #region process

        public Tuple<ShipInformation,ModulesInStore> Process(JournalEntry je, DB.SQLiteConnectionUser conn)
        {
            if (je is IShipInformation)
            {
                IShipInformation e = je as IShipInformation;
                e.ShipInformation(this, conn);                             // not cloned.. up to callers to see if they need to
            }

            return new Tuple<ShipInformation,ModulesInStore>(CurrentShip,StoredModules);
        }

        #endregion
    }
}

