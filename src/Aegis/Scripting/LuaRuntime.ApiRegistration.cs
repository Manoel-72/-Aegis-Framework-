using Aegis.Core;

namespace Aegis.Scripting;

public sealed partial class LuaRuntime
{
    private void Reg(string lua, string method, LuaApiStatus status = LuaApiStatus.Stable)
    {
        _apiStatuses[lua] = status;
        _lua.RegisterFunction(lua, this, GetType().GetMethod(method)!);

        if (status == LuaApiStatus.Experimental)
            WrapExperimentalApi(lua);
    }

    private void RegLegacy(string lua, string method)
        => Reg(lua, method, LuaApiStatus.Legacy);

    private void RegExperimental(string lua, string method)
        => Reg(lua, method, LuaApiStatus.Experimental);

    public void ApiWarning(string apiName, string status)
    {
        if (!string.Equals(status, nameof(LuaApiStatus.Experimental), StringComparison.Ordinal))
            return;

        if (_warnedExperimentalApis.Add(apiName))
            AegisLog.Warn("LuaAPI", $"{apiName} e experimental e pode mudar antes do MVP.");
    }

    private void WrapExperimentalApi(string luaName)
    {
        var dotIndex = luaName.LastIndexOf('.');
        if (dotIndex <= 0 || dotIndex >= luaName.Length - 1)
            return;

        var tableName = luaName[..dotIndex];
        var functionName = luaName[(dotIndex + 1)..];
        var escapedApiName = EscapeLuaString(luaName);
        var escapedStatus = EscapeLuaString(nameof(LuaApiStatus.Experimental));
        var escapedFunctionName = EscapeLuaString(functionName);

        _lua.DoString($$"""
do
  local api_table = {{tableName}}
  local original = api_table["{{escapedFunctionName}}"]
  api_table["{{escapedFunctionName}}"] = function(...)
    aegis.__apiWarning("{{escapedApiName}}", "{{escapedStatus}}")
    return original(...)
  end
end
""");
    }

    private static string EscapeLuaString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
}
