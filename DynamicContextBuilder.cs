using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace EFCoreSandbox
{
    public static class DynamicContextBuilder
    {
        public static Type BuildDynamicDbContextType()
        {
            //Allways get the models before you add teh dynamic type
            var modelTypes = GetTypesToAdd();

            //Set uf some scaffolding
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.Run);

            ModuleBuilder myModBuilder =
                assemblyBuilder.DefineDynamicModule("DynamicContext");

            TypeBuilder typeBuilder = myModBuilder.DefineType("DynamicDbContext",
                TypeAttributes.Public, typeof(DbContext));

            var baseConstructor = typeof(DbContext).GetConstructor(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, null, new Type[] { typeof(DbContextOptions) }, null);


            //Set up the construct that passes the DB Context Options
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(DbContextOptions) });

            var constructorIl = constructor.GetILGenerator();

            constructorIl.Emit(OpCodes.Ldarg_0);                // push "this";
            constructorIl.Emit(OpCodes.Ldarg_1);                // push the 1st parameter
            constructorIl.Emit(OpCodes.Call, baseConstructor);
            //The compiler always adds two nops, not related to the two pushes above.
            constructorIl.Emit(OpCodes.Nop);
            constructorIl.Emit(OpCodes.Nop);
            constructorIl.Emit(OpCodes.Ret);

            foreach (var modelType in modelTypes)
            {
                var shortName = modelType.Name;

                //Create the generic DbSet
                Type dbSetType = typeof(DbSet<>);
                var dbSetGenericType = dbSetType.MakeGenericType(modelType);

                //Backing Field
                FieldBuilder fieldBuilder = typeBuilder.DefineField(shortName.ToLower(),
                    dbSetGenericType,
                    FieldAttributes.Private);

                //Property Builder
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(shortName,
                    PropertyAttributes.HasDefault,
                    dbSetGenericType,
                    null);

                //Getter
                MethodAttributes getSetAttr =
                    MethodAttributes.Public | MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig;

                MethodBuilder getPropBuilder =
                    typeBuilder.DefineMethod($"get_{shortName}",
                        getSetAttr,
                        typeof(string),
                        Type.EmptyTypes);

                ILGenerator getIL = getPropBuilder.GetILGenerator();

                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getIL.Emit(OpCodes.Ret);

                //Setter
                MethodBuilder setPropBuilder =
                    typeBuilder.DefineMethod($"set_{shortName}",
                        getSetAttr,
                        null,
                        new Type[] { typeof(string) });

                ILGenerator setIL = setPropBuilder.GetILGenerator();

                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, fieldBuilder);
                setIL.Emit(OpCodes.Ret);

                //Assign them
                propertyBuilder.SetGetMethod(getPropBuilder);
                propertyBuilder.SetSetMethod(setPropBuilder);
            }

            Type returnType = typeBuilder.CreateType();

            return returnType;
        }

        private static Type[] GetTypesToAdd()
        {
            //Alter this to return the types you want to have added...
            var listOfTypes =
                from assemblyType in Assembly.GetExecutingAssembly().GetExportedTypes()
                where typeof(BaseModel).IsAssignableFrom(assemblyType) && assemblyType != typeof(BaseModel)
                select assemblyType;
            return listOfTypes.ToArray();
        }
    }
}
