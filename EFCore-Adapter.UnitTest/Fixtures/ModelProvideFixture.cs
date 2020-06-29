using NetCasbin;
using NetCasbin.Model;

namespace EFCore_Adapter.UnitTest.Fixtures
{
    public class ModelProvideFixture
    {
        private readonly Model _rbacModel = CoreEnforcer.NewModel("examples/rbac_model.conf", null);

        public Model GetRbacModel()
        {
            return _rbacModel;
        }
    }
}