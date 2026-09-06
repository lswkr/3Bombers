using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public enum AuthState
{
    Authenticating,
    Authenticated,
    NotAuthenticated,
    Error,
    TimeOut
}
public static class Authenticator
{
    public static AuthState State { get; private set; } = AuthState.NotAuthenticated;

    public static async Task<AuthState> DoAuth(int maxTries = 5)
    {
        if (State == AuthState.Authenticating) return State;

        await SignInAnonymouslyAsync(maxTries);

        return State;
    }

    private static async Task SignInAnonymouslyAsync(int maxTries = 5)
    {
        State = AuthState.Authenticating;

        for (int i = 0; i < maxTries; i++)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if(AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    State = AuthState.Authenticated;
                    return;
                }
            }
            catch(AuthenticationException ex)
            {
                Debug.LogError($"인증에 실패했습니다. 에러: {ex.Message}");
                State = AuthState.Error;
                return;
            }
            catch(RequestFailedException ex)
            {
                Debug.LogError($"요청에 실패했습니다. 에러: {ex.Message}");
                State = AuthState.Error;
                return;
            }

            await Task.Delay(1000); //연결 재시도 딜레이
        }
        State = AuthState.TimeOut;
    }
}
