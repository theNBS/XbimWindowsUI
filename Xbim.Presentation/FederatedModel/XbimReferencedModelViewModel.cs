﻿using System.ComponentModel;
using System.IO;
using System.Linq;
using Xbim.Ifc2x3.ActorResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.FederatedModel
{
    /// <summary>
    /// This class can hold all data neccesary to create a XbimReferencedModel.
    /// The purpose of this class is to hold data until it's complete enough to create a XbimReferencedModel. Once created, the model will be preserved in this object
    /// </summary>
    public class XbimReferencedModelViewModel : INotifyPropertyChanged
    {
        #region fields
        XbimReferencedModel _xbimReferencedModel;
        string _identifier = "";
        string _name = "";
        string _organisationName = "";
        string _organisationRole = "";
        #endregion fields

        public XbimReferencedModel ReferencedModel
        {
            get { return _xbimReferencedModel; }
            set { _xbimReferencedModel = value; }
        }

        public string Identifier
        {
            get 
            {
                if (ReferencedModel != null)
                {
                    return ReferencedModel.Identifier;
                }

                return _identifier; 
            }
            //set
            //{
            //    _identifier = value;

            //    if (ReferencedModel != null)
            //    {
            //        ReferencedModel.DocumentInformation.DocumentId = _identifier;
            //    }
            //    OnPropertyChanged("Identifier");
            //}
        }

        public string Name
        {
            get
            {
                if (ReferencedModel != null)
                {
                    return ReferencedModel.DocumentInformation.Name;
                }
                return _name;
            }
            set 
            {
                //can't change the model, once it's created. User should delete and add it again.
                if (ReferencedModel == null)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string OrganisationName
        {
            get
            {
                if (ReferencedModel != null)
                {
                    var organization = ReferencedModel.DocumentInformation.DocumentOwner as IfcOrganization;
                    if (organization != null)
                        return organization.Name;
                }
                return _organisationName;
            }
            set
            {
                if (ReferencedModel != null)
                {
                    var organization = ReferencedModel.DocumentInformation.DocumentOwner as IfcOrganization;
                    if (organization != null)
                    {
                        using (var tnx = ReferencedModel.DocumentInfoTransaction)
                        {
                            organization.Name = value; 
                            tnx.Commit();
                        }
                    }
                }
                _organisationName = value;
                OnPropertyChanged("OrganisationName");
            }
        }

        public string OrganisationRole
        {
            get
            {
                if (ReferencedModel != null)
                {
                    var ownerAsIfcOrganization = ReferencedModel.DocumentInformation.DocumentOwner as IfcOrganization;
                    var roles = ownerAsIfcOrganization.Roles;
                    var role = roles != null ? roles.FirstOrDefault() : null;
                    if (role == null)
                        return "";
                    if (role.Role == IfcRole.UserDefined)
                        return role.UserDefinedRole.ToString();
                    return role.Role.ToString();
                }
                return _organisationRole;
            }
            set
            {
                _organisationRole = value;

                if (ReferencedModel != null)
                {
                    var ownerAsIfcOrganization = ReferencedModel.DocumentInformation.DocumentOwner as IfcOrganization;
                    var role = ownerAsIfcOrganization.Roles.FirstOrDefault(); // assumes the first to be modified
                    using (var tnx = ReferencedModel.DocumentInfoTransaction)
                    {
                        role.RoleString = value; // the string is converted appropriately by the IfcActorRoleClass
                        tnx.Commit();
                    }
                }
                OnPropertyChanged("OrganisationRole");
            }
        }

        public XbimReferencedModelViewModel()
        {
            //default constructor to create objects without model
        }

        public XbimReferencedModelViewModel(XbimReferencedModel model)
        {
            ReferencedModel = model;
        }

        /// <summary>
        /// Validates all data and creates model. 
        /// Provide a "XbimModel model = DataContext as XbimModel;"
        /// </summary>
        /// <returns>Returns XbimReferencedModel == null </returns>
        public bool TryBuild(XbimModel model)
        {
            //it's already build, so no need to recreate it
            if (ReferencedModel != null)
                return true;

		    if (string.IsNullOrWhiteSpace(Name))
                return false;
            string ext = Path.GetExtension(Name).ToLowerInvariant();
            using (XbimModel refM = new XbimModel())
            {
                if (ext != ".xbim")
                {
                    refM.CreateFrom(Name, null, null, true);
                    var m3D = new Xbim3DModelContext(refM);
                    m3D.CreateContext();
                    Name = Path.ChangeExtension(Name, "xbim");
                }
            }

            _xbimReferencedModel = model.AddModelReference(Name, OrganisationName, OrganisationRole);

            if (_xbimReferencedModel != null)
            {
                //refresh all
                OnPropertyChanged("Identifier");
                OnPropertyChanged("Name");
                OnPropertyChanged("OrganisationName");
                OnPropertyChanged("OrganisationRole");
            }
            return ReferencedModel != null;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
