using System;
using System.Linq;
using System.Threading.Tasks;
using Casbin.NET.Adapter.EFCore;
using EFCore_Adapter.UnitTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using NetCasbin;
using Xunit;

namespace EFCore_Adapter.UnitTest
{
    public class AdapterTest : TestUtil,IClassFixture<ModelProvideFixture>, IDisposable
    {
        private readonly ModelProvideFixture _modelProvideFixture;
        private readonly CasbinDbContext<int> _context;
        private readonly CasbinDbContext<int> _asyncContext;

        public AdapterTest(ModelProvideFixture modelProvideFixture)
        {
            _modelProvideFixture = modelProvideFixture;
            var options = new DbContextOptionsBuilder<CasbinDbContext<int>>()
                .UseSqlite("Data Source=casbin_test.sqlite3")
                .Options;

            var asyncOptions = new DbContextOptionsBuilder<CasbinDbContext<int>>()
                .UseSqlite("Data Source=casbin_async_test.sqlite3")
                .Options;

            _context = new CasbinDbContext<int>(options);
            _context.Database.EnsureCreated();
            _asyncContext = new CasbinDbContext<int>(asyncOptions);
            _asyncContext.Database.EnsureCreated();

            InitPolicy(_context);
            InitPolicy(_asyncContext);
        }

        public void Dispose()
        {
            Dispose(_context);
            Dispose(_asyncContext);
        }

        private void Dispose(CasbinDbContext<int> context)
        {
            context.RemoveRange(context.CasbinRule);
            context.SaveChanges();
        }

        private static void InitPolicy(CasbinDbContext<int> context)
        {
            context.CasbinRule.Add(new CasbinRule<int>()
            {
                PType = "p",
                V0 = "alice",
                V1 = "data1",
                V2 = "read",
            });
            context.CasbinRule.Add(new CasbinRule<int>()
            {
                PType = "p",
                V0 = "bob",
                V1 = "data2",
                V2 = "write",
            });
            context.CasbinRule.Add(new CasbinRule<int>()
            {
                PType = "p",
                V0 = "data2_admin",
                V1 = "data2",
                V2 = "read",
            });
            context.CasbinRule.Add(new CasbinRule<int>()
            {
                PType = "p",
                V0 = "data2_admin",
                V1 = "data2",
                V2 = "write",
            });
            context.CasbinRule.Add(new CasbinRule<int>()
            {
                PType = "g",
                V0 = "alice",
                V1 = "data2_admin",
            });
            context.SaveChanges();
        }

        [Fact]
        public void TestAdapterAutoSave()
        {
            var efAdapter = new CasbinDbAdapter<int>(_context);
            var e = new Enforcer(_modelProvideFixture.GetRbacModel(), efAdapter);

            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(_context.CasbinRule.Count() == 5);

            e.AddPolicy("alice", "data1", "write");
            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data1", "write")
            ));
            Assert.True(_context.CasbinRule.Count() == 6);

            e.RemovePolicy("alice", "data1", "write");
            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(_context.CasbinRule.Count() == 5);

            e.RemoveFilteredPolicy(0, "data2_admin");
            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write")
            ));
            Assert.True(_context.CasbinRule.Count() == 3);
        }

        [Fact]
        public async Task TestAdapterAutoSaveAsync()
        {
            var efAdapter = new CasbinDbAdapter<int>(_asyncContext);
            var e = new Enforcer(_modelProvideFixture.GetRbacModel(), efAdapter);

            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(_asyncContext.CasbinRule.Count() == 5);

            await e.AddPolicyAsync("alice", "data1", "write");
            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write"),
                AsList("alice", "data1", "write")
            ));
            Assert.True(_asyncContext.CasbinRule.Count() == 6);

            await e.RemovePolicyAsync("alice", "data1", "write");
            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write"),
                AsList("data2_admin", "data2", "read"),
                AsList("data2_admin", "data2", "write")
            ));
            Assert.True(_asyncContext.CasbinRule.Count() == 5);

            await e.RemoveFilteredPolicyAsync(0, "data2_admin");
            TestGetPolicy(e, AsList(
                AsList("alice", "data1", "read"),
                AsList("bob", "data2", "write")
            ));
            Assert.True(_asyncContext.CasbinRule.Count() == 3);
        }
    }
}
