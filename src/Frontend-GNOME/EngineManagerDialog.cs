/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.Collections;
using System.Collections.Specialized;
using Mono.Unix;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class EngineManagerDialog : Gtk.Dialog
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Gtk.ComboBox  _ComboBox;
        private Gtk.ListStore _ListStore;
        private string        _SelectedEngine;  
        private EngineManager _EngineManager;
        private Gtk.Button    _EditButton;
        private Gtk.Button    _DeleteButton;
        
        public EngineManagerDialog(EngineManager engineManager)
        {
            Trace.Call(engineManager);
            
            if (engineManager == null) {
                throw new ArgumentNullException("engineManager");
            }
            
            _EngineManager = engineManager;
            
            Modal = true;
            Title = "smuxi - " + _("Engine Manager");
                
            Gtk.HBox connect_hbox = new Gtk.HBox();
            Gtk.Image connect_image = new Gtk.Image(new Gdk.Pixbuf(null,
                "connect.png"));
            connect_hbox.Add(connect_image);
            connect_hbox.Add(new Gtk.Label(_("_Connect")));
            Gtk.Button connect_button = new Gtk.Button(connect_hbox);
            AddActionWidget(connect_button, 1);
            
            AddActionWidget(new Gtk.Button(Gtk.Stock.New), 3);
            
            Gtk.HBox edit_hbox = new Gtk.HBox();
            Gtk.Image edit_image = new Gtk.Image(new Gdk.Pixbuf(null,
                "edit.png"));
            edit_hbox.Add(edit_image);
            edit_hbox.Add(new Gtk.Label(Catalog.GetString("_Edit")));
            Gtk.Button edit_button = new Gtk.Button(edit_hbox);
            _EditButton = edit_button;
            AddActionWidget(edit_button, 2);
            
            _DeleteButton = new Gtk.Button(Gtk.Stock.Delete);
            AddActionWidget(_DeleteButton, 4);
            AddActionWidget(new Gtk.Button(Gtk.Stock.Quit), 5);
            Response += new Gtk.ResponseHandler(_OnResponse);
            
            Gtk.VBox vbox = new Gtk.VBox();
            Gtk.Label label = new Gtk.Label("<b>" + 
                                            Catalog.GetString("Select to which smuxi engine you want to connect") +
                                            "</b>");
            label.UseMarkup = true;
            vbox.PackStart(label, false, false, 5);
            
            Gtk.HBox hbox = new Gtk.HBox();
            hbox.PackStart(new Gtk.Label(Catalog.GetString("Engine:")), false, false, 5);
            
            _ListStore = new Gtk.ListStore(typeof(string));
            _ComboBox = new Gtk.ComboBox();
            Gtk.CellRendererText cell = new Gtk.CellRendererText();
            _ComboBox.PackStart(cell, false);
            _ComboBox.AddAttribute(cell, "text", 0);
            _ComboBox.Changed += new EventHandler(_OnComboBoxChanged);
            _ComboBox.Model = _ListStore;
            _InitEngineList();

            hbox.PackStart(_ComboBox, true, true, 10); 
            
            vbox.PackStart(hbox, false, false, 10);
            
            VBox.Add(vbox);
            
            ShowAll();
        }
        
        private void _InitEngineList()
        {
            string[] engines = (string[])Frontend.FrontendConfig["Engines/Engines"];
            string default_engine = (string)Frontend.FrontendConfig["Engines/Default"];
            int item = 0;
            _ListStore.Clear();
            _ListStore.AppendValues("<" + _("Local Engine") + ">");
            item++;
            foreach (string engine in engines) {
                _ListStore.AppendValues(engine);
                if (engine == default_engine) {
                    _ComboBox.Active = item;
                }
                item++;
            }
        }
        
        private void _OnResponse(object sender, Gtk.ResponseArgs e)
        {
            Trace.Call(sender, e);
            
            try {
#if LOG4NET
                _Logger.Debug("_OnResponse(): ResponseId: "+e.ResponseId);
#endif                
                switch ((int)e.ResponseId) {
                    case 1:
                        _OnConnectButtonPressed();
                        break;
#if UI_GNOME
                    case 2:
                        _OnEditButtonPressed();
                        break;
                    case 3:
                        _OnNewButtonPressed();
                        break;
#endif
                    case 4:
                        _OnDeleteButtonPressed();
                        break;
                    case 5:
                        _OnQuitButtonPressed();
                        break;
                    case (int)Gtk.ResponseType.DeleteEvent:
                        _OnDeleteEvent();
                        break;
                    default:
                        NotImplementedMessageDialog nid = new NotImplementedMessageDialog(this);
                        nid.Run();
                        nid.Destroy();
                        
                        // Re-run the Dialog
                        Run();
                        break;
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
                CrashDialog.Show(this, ex);
            }
        }
        
        private void _OnConnectButtonPressed()
        {
            if (_SelectedEngine == null || _SelectedEngine == String.Empty) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(this,
                    Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                    Gtk.ButtonsType.Close, Catalog.GetString("Please select an engine!"));
                md.Run();
                md.Destroy();
                // Re-run the Dialog
                Run();
                return;
            }
            
            if (_SelectedEngine == "<" + _("Local Engine") + ">") {
                Frontend.InitLocalEngine();
                Destroy();
                return;
            }

            string engine = _SelectedEngine;
            try {
                _EngineManager.Connect(engine);
                if (_EngineManager.EngineVersion.Major != Frontend.Version.Major ||
                    _EngineManager.EngineVersion.Minor != Frontend.Version.Minor ||
                    _EngineManager.EngineVersion.Build != Frontend.Version.Build) {                        
                    throw new ApplicationException(String.Format(
                                _("Your frontend version ({0}) is not matching the engine version ({1})!"),
                                Frontend.Version, _EngineManager.EngineVersion));
                }
                
                Frontend.Session = _EngineManager.Session;
                Frontend.UserConfig = _EngineManager.UserConfig;
                Frontend.EngineVersion = _EngineManager.EngineVersion;
                Frontend.ConnectEngineToGUI();
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif

                string error_msg = ex.Message + "\n";
                if (ex.InnerException != null) {
                    error_msg += " [" + ex.InnerException.Message + "]\n";
                }
                
                string msg;
                msg = _("Error occured while connecting to the engine!") + "\n\n";
                msg += String.Format(_("Engine URL: {0}") + "\n",
                                     _EngineManager.EngineUrl);
                
                msg += String.Format(_("Error: {0}"), error_msg);
                
                Gtk.MessageDialog md = new Gtk.MessageDialog(this, Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Error, Gtk.ButtonsType.Close, msg);
                md.Run();
                md.Destroy();
                
                // Re-run the Dialog
                Run();
            }
        }

#if UI_GNOME
        private void _OnNewButtonPressed()
        {
            // the druid will spawn EngineManagerDialog when it's canceled or finished
            Destroy();
            new EngineDruid(Frontend.FrontendConfig);
        }
#endif

#if UI_GNOME
        private void _OnEditButtonPressed()
        {
            // the druid will spawn EngineManagerDialog when it's canceled or finished
            Destroy();
            new EngineDruid(Frontend.FrontendConfig, _SelectedEngine);
        }
#endif
        
        private void _OnDeleteButtonPressed()
        {
            string msg = String.Format(
                            _("Are you sure you want to delete the engine \"{0}\"?"),
                            _SelectedEngine);
            Gtk.MessageDialog md = new Gtk.MessageDialog(this, Gtk.DialogFlags.Modal,
            Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo, msg);
            int res = md.Run();
            md.Destroy();
            if ((Gtk.ResponseType)res == Gtk.ResponseType.Yes) {
                _DeleteEngine(_SelectedEngine);
                _InitEngineList();
            }
        }
        
        private void _DeleteEngine(string engine)
        {
            StringCollection new_engines = new StringCollection();
            string[] current_engines = (string[])Frontend.FrontendConfig["Engines/Engines"];
            foreach (string eng in current_engines) {
                if (eng != engine) {
                    new_engines.Add(eng);
                }
            }
            string[] new_engines_array = new string[new_engines.Count]; 
            new_engines.CopyTo(new_engines_array, 0);
            Frontend.FrontendConfig["Engines/Engines"] = new_engines_array;
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Username");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Password");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Hostname");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Port");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Channel");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Formatter");
            Frontend.FrontendConfig.Remove("Engines/"+engine);
            Frontend.FrontendConfig.Save();
            Frontend.FrontendConfig.Load();
        }
        
        private void _OnQuitButtonPressed()
        {
            Frontend.Quit();
        }
        
        private void _OnDeleteEvent()
        {
            Frontend.Quit();
        }
        
        private void _OnComboBoxChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            Gtk.TreeIter iter;
            if (_ComboBox.GetActiveIter(out iter)) {
               _SelectedEngine = (string )_ComboBox.Model.GetValue(iter, 0);
            }
            
            bool isLocalEngine = _SelectedEngine == "<" + _("Local Engine") + ">";
            _EditButton.Sensitive = !isLocalEngine; 
            _DeleteButton.Sensitive = !isLocalEngine; 
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
