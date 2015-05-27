﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    IfcMetaDataControl.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;

#endregion

namespace Xbim.Presentation
{
    /// <summary>
    ///   Interaction logic for IfcMetaDataControl.xaml
    /// </summary>
    public partial class IfcMetaDataControl : INotifyPropertyChanged
    {
        public class PropertyItem 
        {
            public string Units { get; set; }

            public string PropertySetName { get; set; }

            public string Name { get; set; }
            
            public string Value { get; set; }

            private readonly string[] _schemas = { "file", "ftp", "http", "https" };
            public bool IsLink 
            {
                get
                {
                    Uri uri;
                    if (!Uri.TryCreate(Value, UriKind.Absolute, out uri)) 
                        return false;
                    var schema = uri.Scheme;
                    return _schemas.Contains(schema);
                }
            }
        }

        private IPersistIfcEntity _entity;
        public IfcMetaDataControl()
        {
            InitializeComponent();
            TheTabs.SelectionChanged += TheTabs_SelectionChanged;
            _propertyGroups = new ListCollectionView(_properties);
            _propertyGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
            _propertyGroups.SortDescriptions.Add(new SortDescription("PropertySetName", ListSortDirection.Ascending));
            _materialGroups = new ListCollectionView(_materials);
            _materialGroups.GroupDescriptions.Add(new PropertyGroupDescription("PropertySetName"));
        }

        void TheTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) 
                return;
            var selectedTab = e.AddedItems[0] as TabItem;  // Gets selected tab
            FillTabValues(selectedTab);
        }

        private void FillTabValues(TabItem selectedTab)
        {
            //only fill tabs on demand when they are activated
            if (selectedTab == null)
                return;

            // ReSharper disable PossibleUnintendedReferenceComparison
            if (selectedTab == ObjectTab)
                FillObjectData();
            else if (selectedTab == TypeTab)
                FillTypeData();
            else if (selectedTab == PropertyTab)
                FillPropertyData();
            else if (selectedTab == QuantityTab)
                FillQuantityData();
            else if (selectedTab == MaterialTab)
                FillMaterialData();
            // ReSharper restore PossibleUnintendedReferenceComparison
        }

        private readonly ListCollectionView _propertyGroups;

        public ListCollectionView PropertyGroups
        {
            get { return _propertyGroups; }
        }
        private readonly ListCollectionView _materialGroups;

        public ListCollectionView MaterialGroups
        {
            get { return _materialGroups; }
        }
       
        private readonly ObservableCollection<PropertyItem> _objectProperties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> ObjectProperties
        {
            get { return _objectProperties; }
        }
        private readonly ObservableCollection<PropertyItem> _quantities = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Quantities
        {
            get { return _quantities; }
        }
        private readonly ObservableCollection<PropertyItem> _properties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Properties
        {
            get { return _properties; }
            
        }

        private readonly ObservableCollection<PropertyItem> _materials = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Materials
        {
            get { return _materials; }
        }

        private readonly ObservableCollection<PropertyItem> _typeProperties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> TypeProperties
        {
            get { return _typeProperties; }
        }

        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IfcInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(IfcMetaDataControl),
                                        new UIPropertyMetadata(null, OnSelectedEntityChanged));


        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as IfcMetaDataControl;
            if (ctrl != null && e.NewValue != null && e.NewValue is IPersistIfcEntity)
            {
                ctrl.DataRebind((IPersistIfcEntity)e.NewValue);
            }

            //ctrl.SetUpLinks();
        }

        private void DataRebind(IPersistIfcEntity entity)
        {
            Clear(); //remove any bindings
           // ScrollView.ScrollToHome();
            _entity = null;

            if (entity != null)
            {
                _entity = entity;
                FillTabValues(TheTabs.SelectedItem as TabItem);
            }
            else
                _entity = null;
        }

        private void FillTypeData()
        {
            if (_typeProperties.Count > 0) return; //don't fill unless empty
            var ifcObj = _entity as IfcObject;
            if (ifcObj == null) 
                return;
            var typeEntity = ifcObj.GetDefiningType();
            if (typeEntity == null) 
                return;
            var ifcType = IfcMetaData.IfcType(typeEntity);
            _typeProperties.Add(new PropertyItem { Name = "Type", Value = ifcType.Type.Name });
            _typeProperties.Add(new PropertyItem { Name = "Ifc Label", Value = "#" + typeEntity.EntityLabel });

            _typeProperties.Add(new PropertyItem { Name = "Name", Value = typeEntity.Name });
            _typeProperties.Add(new PropertyItem { Name = "Description", Value = typeEntity.Description });
            _typeProperties.Add(new PropertyItem { Name = "GUID", Value = typeEntity.GlobalId });
            _typeProperties.Add(new PropertyItem
            {
                Name = "Ownership",
                Value = typeEntity.OwnerHistory.OwningUser + " using " + typeEntity.OwnerHistory.OwningApplication.ApplicationIdentifier
            });
            //now do properties in further specialisations that are text labels
            foreach (var pInfo in ifcType.IfcProperties.Where
                (p => p.Value.IfcAttribute.Order > 4
                      && p.Value.IfcAttribute.State != IfcAttributeState.DerivedOverride)
                ) //skip the first for of root, and derived and things that are objects
            {
                var val = pInfo.Value.PropertyInfo.GetValue(typeEntity, null);
                if (val == null || !(val is ExpressType)) 
                    continue;
                var pi = new PropertyItem { Name = pInfo.Value.PropertyInfo.Name, Value = ((ExpressType)val).ToPart21 };
                _typeProperties.Add(pi);
            }
        }

        private void FillQuantityData()
        {
            if (_quantities.Count > 0) return; //don't fill unless empty
            //now the property sets for any 
            
            // local cache for efficiency

            if (_entity is IfcObject)
            {
                var ifcObj = _entity as IfcObject;
                var modelUnits = _entity.ModelOf.Instances.OfType<IfcUnitAssignment>().FirstOrDefault(); // not optional, should never return void in valid model
                
                foreach (var relDef in ifcObj.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition is IfcElementQuantity))
                {
                    var pSet = relDef.RelatingPropertyDefinition as IfcElementQuantity;
                    AddQuantityPSet(pSet, modelUnits);
                }
            }
            else if (_entity is IfcTypeObject)
            {
                var ifcObj = _entity as IfcTypeObject;
                var modelUnits = _entity.ModelOf.Instances.OfType<IfcUnitAssignment>().FirstOrDefault(); // not optional, should never return void in valid model

                var asIfcTypeObject = _entity as IfcTypeObject;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IfcElementQuantity>())
                {
                    AddQuantityPSet(pSet, modelUnits);
                }

                //foreach (var relDef in ifcObj. IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition is IfcElementQuantity))
                //{
                //    var pSet = relDef.RelatingPropertyDefinition as IfcElementQuantity;
                //    AddQuantityPSet(pSet, modelUnits);
                //}
            }
        }

        private void AddQuantityPSet(IfcElementQuantity pSet, IfcUnitAssignment modelUnits)
        {
            if (pSet == null) 
                return;
            foreach (var item in pSet.Quantities.OfType<IfcPhysicalSimpleQuantity>()) // currently only handles IfcPhysicalSimpleQuantity
            {  
                var v = modelUnits.GetUnitFor(item);
                _quantities.Add(new PropertyItem
                {
                    PropertySetName = pSet.Name,
                    Name = item.Name,
                    Value = item + " " + v.GetName()
                });
            }
        }

        private void FillPropertyData()
        {
            if (_properties.Any()) //don't try to fill unless empty
                return; 
            //now the property sets for any 
            
            if (_entity is IfcObject)
            {
                var asIfcObject = _entity as IfcObject;
                foreach (
                    var pSet in
                        asIfcObject.IsDefinedByProperties.Select(
                            relDef => relDef.RelatingPropertyDefinition as IfcPropertySet))
                    AddPropertySet(pSet);
            }
            else if (_entity is IfcTypeObject)
            {
                var asIfcTypeObject = _entity as IfcTypeObject;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IfcPropertySet>())
                {
                    AddPropertySet(pSet);
                }
            }

        }

        private void AddPropertySet(IfcPropertySet pSet)
        {
            if (pSet == null)
                return;
            foreach (var item in pSet.HasProperties.OfType<IfcPropertySingleValue>()) //only handle simple properties
            {
                var val = "";
                if (item.NominalValue != null)
                {
                    var nomVal = (ExpressType) (item.NominalValue);
                    val = nomVal.Value != null
                        ? nomVal.Value.ToString()
                        : item.NominalValue.ToString();
                }
                _properties.Add(new PropertyItem
                {
                    PropertySetName = pSet.Name,
                    Name = item.Name,
                    Value = val
                });
            }
        }

        private void FillMaterialData()
        {
            if (_materials.Any()) 
                return; //don't fill unless empty
            
            if (_entity is IfcObject)
            {
                var ifcObj = _entity as IfcObject;
                var matRels = ifcObj.HasAssociations.OfType<IfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
            else if (_entity is IfcTypeObject)
            {
                var ifcObj = _entity as IfcTypeObject;
                var matRels = ifcObj.HasAssociations.OfType<IfcRelAssociatesMaterial>();
                foreach (var matRel in matRels)
                {
                    AddMaterialData(matRel.RelatingMaterial, "");
                }
            }
        }

        private void AddMaterialData(IfcMaterialSelect matSel, string setName)
        {
            if (matSel is IfcMaterial) //simplest just add it
                _materials.Add(new PropertyItem
                {
                    Name = string.Format("{0} [#{1}]", ((IfcMaterial)matSel).Name, matSel.EntityLabel),
                    PropertySetName = setName,
                    Value = ""
                });
            else if (matSel is IfcMaterialLayer)
                _materials.Add(new PropertyItem
                {
                    Name = string.Format("{0} [#{1}]", ((IfcMaterialLayer)matSel).Material.Name, matSel.EntityLabel),
                    Value = ((IfcMaterialLayer)matSel).LayerThickness.Value.ToString(),
                    PropertySetName = setName
                });
            else if (matSel is IfcMaterialList)
            {
                foreach (var mat in ((IfcMaterialList)matSel).Materials)
                {
                    _materials.Add(new PropertyItem
                    {
                        Name = string.Format("{0} [#{1}]", mat.Name, mat.EntityLabel),
                        PropertySetName = setName,
                        Value = ""
                    });
                }
            }
            else if (matSel is IfcMaterialLayerSet)
            {
                foreach (var item in ((IfcMaterialLayerSet)matSel).MaterialLayers) //recursive call to add materials
                {
                    AddMaterialData(item, ((IfcMaterialLayerSet)matSel).LayerSetName);
                }
            }
            else if (matSel is IfcMaterialLayerSetUsage)
            {
                foreach (var item in ((IfcMaterialLayerSetUsage)matSel).ForLayerSet.MaterialLayers) //recursive call to add materials
                {
                    AddMaterialData(item, ((IfcMaterialLayerSetUsage)matSel).ForLayerSet.LayerSetName);
                }
            }
        }

        private void FillObjectData()
        {
            if (_objectProperties.Count > 0) return; //don't fill unless empty
            if (_entity != null)
            {
                var ifcType = IfcMetaData.IfcType(_entity);
                _objectProperties.Add(new PropertyItem { Name = "Type", Value = ifcType.Type.Name });
                _objectProperties.Add(new PropertyItem { Name = "Ifc Label", Value = "#" + _entity.EntityLabel});
                var root = _entity as IfcRoot;
                if (root !=null) //should always be really
                {
                    _objectProperties.Add(new PropertyItem { Name = "Name", Value = root.Name });
                    _objectProperties.Add(new PropertyItem { Name = "Description", Value = root.Description });
                    _objectProperties.Add(new PropertyItem { Name = "GUID", Value = root.GlobalId });
                    _objectProperties.Add(new PropertyItem
                    { Name = "Ownership", 
                        Value = root.OwnerHistory.OwningUser + " using " +root.OwnerHistory.OwningApplication.ApplicationIdentifier });
                    //now do properties in further specialisations that are text labels
                    foreach (var pInfo in ifcType.IfcProperties.Where
                        (p => p.Value.IfcAttribute.Order > 4
                         && p.Value.IfcAttribute.State != IfcAttributeState.DerivedOverride)
                        ) //skip the first for of root, and derived and things that are objects
                    {                      
                        var val = pInfo.Value.PropertyInfo.GetValue(_entity, null);
                        if (val == null || !(val is ExpressType)) 
                            continue;
                        var pi = new PropertyItem { Name = pInfo.Value.PropertyInfo.Name, Value = ((ExpressType)val).ToPart21 };
                        _objectProperties.Add(pi);
                    }
                }
            }
        }

        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(IfcMetaDataControl), new PropertyMetadata(null, OnModelChanged));


        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as IfcMetaDataControl;
            if (ctrl != null)
            {
                if (e.NewValue == null)
                {
                    ctrl.Clear();
                }
                ctrl.DataRebind(null);
            }
        }


        private void Clear()
        {
            _objectProperties.Clear();
            _quantities.Clear();
            _properties.Clear() ;
            _typeProperties.Clear();
            _materials.Clear();
            NotifyPropertyChanged("Properties");
            NotifyPropertyChanged("PropertySets");
        }

        //////private void LoadMetaData(IPersistIfcEntity item)
        //////{
        //////    IfcType ifcType = item.IfcType();


        //////    List<PropertyItem> pis = new List<PropertyItem>(ifcType.IfcProperties.Count());
        //////    PropertyItem plabel = new PropertyItem()
        //////                              {ID = 0, Name = "Label", Value = "#" + Math.Abs(item.EntityLabel).ToString()};
        //////    pis.Add(plabel);
        //////    foreach (var pInfo in ifcType.IfcProperties)
        //////    {
        //////        if (pInfo.Value.IfcAttribute.State != IfcAttributeState.DerivedOverride)
        //////            //only process Ifc Properties and those that are not derived
        //////        {
        //////            PropertyItem pi = new PropertyItem()
        //////                                  {ID = pInfo.Value.IfcAttribute.Order, Name = pInfo.Value.PropertyInfo.Name};
        //////            pis.Add(pi);
        //////            object val = pInfo.Value.PropertyInfo.GetValue(item, null);
        //////            if (val != null)
        //////            {
        //////                if (val is ExpressType)
        //////                    pi.Value = ((ExpressType) val).ToPart21;
        //////                else if (val.GetType().IsEnum)
        //////                    pi.Value = string.Format(".{0}.", val.ToString());
        //////                else //it's a class
        //////                {
        //////                    pi.Value = string.Format("{0}", pInfo.Value.PropertyInfo.PropertyType.Name.ToUpper());
        //////                }
        //////            }
        //////            else
        //////                pi.Value = "null";
        //////        }
        //////    }

        //////    _properties = new ObservableCollection<PropertyItem>(pis);
        //////    NotifyPropertyChanged("Properties");
        //////    IfcObject ifcObj = item as IfcObject;
            
        //////    if (ifcObj != null)
        //////    {
        //////        IModel m = ifcObj.ModelOf;
        //////        //write out any material layers
        //////        IEnumerable<IfcRelAssociatesMaterial> matRels =
        //////            ifcObj.HasAssociations.OfType<IfcRelAssociatesMaterial>();
        //////        foreach (IfcRelAssociatesMaterial matRel in matRels)
        //////        {
        //////            _materials.Add(matRel.RelatingMaterial);
        //////        }
        //////        //now the property sets
        //////        foreach (IfcRelDefinesByProperties relDef in ifcObj.IsDefinedByProperties)
        //////        {
        //////            IfcPropertySet pSet = relDef.RelatingPropertyDefinition as IfcPropertySet;
        //////            if (pSet != null)
        //////                _propertySets.Add(pSet);
        //////        }
        //////        ////now the type property sets
        //////        //IfcTypeObject to = ifcObj.GetDefiningType(m);
        //////        //if (to != null)
        //////        //{
        //////        //    PropertySetDefinitionSet pds = to.HasPropertySets;
        //////        //    if (pds != null)
        //////        //    {
        //////        //        foreach (IfcPropertySetDefinition pSet in pds)
        //////        //        {
        //////        //            _typePropertySets.Add(pSet);
        //////        //        }
        //////        //    }
        //////        //}
        //////    }
        //////    NotifyPropertyChanged("PropertySets");
        //////}

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink == null)
                throw new ArgumentNullException();
            Process.Start(e.Uri.OriginalString);
        }
    }
}