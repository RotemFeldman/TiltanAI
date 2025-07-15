using System.Collections.Generic;

public interface IGoapState
{
    void SetState(string key, object value);
    object GetState(string key);
    bool HasState(string key);
    void RemoveState(string key);
    Dictionary<string, object> GetAllStates();
    IGoapState Clone();
    bool Equals(IGoapState other);
}
