using System.IO;
using System.Windows;
using Caliburn.Testability;
using Rhino.Licensing.AdminTool.Model;
using Rhino.Licensing.AdminTool.Services;
using Rhino.Licensing.AdminTool.ViewModels;
using Rhino.Licensing.AdminTool.Views;
using Rhino.Mocks;
using Xunit;
using Caliburn.Testability.Extensions;

namespace Rhino.Licensing.AdminTool.Tests.ViewModels
{
    public class ProjectViewModelTests
    {
        private readonly IDialogService _dialogService;
        private readonly IProjectService _projectService;

        public ProjectViewModelTests()
        {
            _dialogService = MockRepository.GenerateMock<IDialogService>();
            _projectService = MockRepository.GenerateMock<IProjectService>();
        }

        [Fact]
        public void Creating_New_ProductViewModel_Will_Have_Empty_Product()
        {
            var vm = CreateViewModel();
            
            Assert.Null(vm.CurrentProject);
        }

        [Fact]
        public void Fires_PropertyChange_Notification()
        {
            var vm = CreateViewModel();

            vm.AssertThatProperty(x => x.CurrentProject).RaisesChangeNotification();
        }

        [Fact]
        public void Properties_Are_Bound()
        {
            var validator = Validator.For<ProjectView, ProjectViewModel>()
                              .Validate();

            validator.AssertWasBound(x => x.CurrentProject.Product.Name);
            validator.AssertWasBound(x => x.CurrentProject.Product.PrivateKey);
            validator.AssertWasBound(x => x.CurrentProject.Product.PublicKey);
        }

        [Fact]
        public void Can_Not_Save_If_Name_Is_Not_Provided()
        {
            var vm = CreateViewModel();

            vm.CurrentProject = new Project
            {
                Product = new Product
                {
                    Name = null
                }
            };

            Assert.False(vm.CanSave());
        }

        [Fact]
        public void Can_Save_If_Name_Is_Provided()
        {
            var vm = CreateViewModel();
            
            vm.CurrentProject = new Project
            {
                Product = new Product
                {
                    Name = "New Product"
                }
            };

            Assert.True(vm.CanSave());
        }

        [Fact]
        public void Save_Action_Will_Open_SaveDialog()
        {
            var dialogModel = new SaveFileDialogViewModel {Result = true};
            _dialogService.Expect(x => x.ShowSaveFileDialog(dialogModel));

            var vm = CreateViewModel(dialogModel);
            vm.Save();

            _dialogService.AssertWasCalled(x => x.ShowSaveFileDialog(Arg.Is(dialogModel)), x => x.Repeat.Once());
        }

        [Fact]
        public void Save_Action_Will_Call_ProjectService_If_Proper_Result_Is_Set()
        {
            var existingFile = Path.GetTempFileName();
            var choosenFile = new FileInfo(existingFile);
            var model = new SaveFileDialogViewModel {Result = true, FileName = existingFile};

            _dialogService.Expect(x => x.ShowSaveFileDialog(model));

            var vm = CreateViewModel(model);
            vm.Save();

            _projectService.Expect(x => x.Save(Arg<Project>.Is.Anything, Arg.Is(choosenFile))).Repeat.Once();
        }

        [Fact]
        public void Will_Not_Proceed_To_Save_When_No_File_Is_Selected()
        {
            var dialogModel = new SaveFileDialogViewModel {Result = true, FileName = null};
            _dialogService.Expect(x => x.ShowSaveFileDialog(dialogModel));

            var vm = CreateViewModel(dialogModel);
            vm.Save();
            
            _projectService.AssertWasNotCalled(x => x.Save(Arg<Project>.Is.Anything, Arg<FileInfo>.Is.Anything));
        }

        [Fact]
        public void Can_Generate_Key_Pair()
        {
            var vm = CreateViewModel();

            vm.CurrentProject = new Project {Product = new Product()};
            vm.GenerateKey();

            Assert.NotNull(vm.CurrentProject.Product.PublicKey);
            Assert.NotNull(vm.CurrentProject.Product.PrivateKey);
            Assert.Contains("<P>", vm.CurrentProject.Product.PrivateKey); //Makes sure it is only private
            Assert.Contains("<Modulus>", vm.CurrentProject.Product.PublicKey); //Makes sure it is public
        }

        [Fact]
        public void Default_Project_Save_Dialog()
        {
            var vm = new ProjectViewModel(_projectService, _dialogService);
            var dialogModel = vm.CreateSaveDialogModel();

            Assert.Equal("Rhino License|*.rlic", dialogModel.Filter);
            Assert.True(dialogModel.OverwritePrompt);
        }

        [Fact]
        public void Can_Copy_Keys_To_Clipboard()
        {
            var keyContent = "Key Content";
            var vm = CreateViewModel();

            vm.CopyToClipboard(keyContent);

            var readback = Clipboard.GetText(TextDataFormat.UnicodeText);

            Assert.Equal(keyContent, readback);
        }

        private ProjectViewModel CreateViewModel(ISaveFileDialogViewModel model)
        {
            return new TestProjectViewModel(_projectService, _dialogService, model);
        }

        private ProjectViewModel CreateViewModel()
        {
            return new TestProjectViewModel(_projectService, _dialogService, null);
        }

        public class TestProjectViewModel : ProjectViewModel
        {
            private readonly ISaveFileDialogViewModel _dialogModel;

            public TestProjectViewModel(IProjectService projectService, IDialogService dialogService, ISaveFileDialogViewModel model) 
                : base(projectService, dialogService)
            {
                _dialogModel = model;
            }

            public override ISaveFileDialogViewModel CreateSaveDialogModel()
            {
                return _dialogModel; 
            }
        }
    }
}