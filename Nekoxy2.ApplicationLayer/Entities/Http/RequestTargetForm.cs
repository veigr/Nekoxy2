namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// リクエストターゲットの形式
    /// </summary>
    internal enum RequestTargetForm
    {
        Unknown,
        OriginForm,
        AbsoluteForm,
        AuthorityForm,
        AsteriskForm,
    }
}
