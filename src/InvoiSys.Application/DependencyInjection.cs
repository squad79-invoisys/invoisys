using FluentValidation;
using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Application.Autenticacao.Logout;
using InvoiSys.Application.Autenticacao.Refresh;
using InvoiSys.Application.Coletas.ConsultarColeta;
using InvoiSys.Application.Coletas.ExecutarColetaManual;
using InvoiSys.Application.Coletas.ListarColetas;
using InvoiSys.Application.Documentos.ConsultarDocumento;
using InvoiSys.Application.Documentos.ListarDocumentos;
using InvoiSys.Application.Fontes.AlterarStatusFonte;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Application.Fontes.ConsultarFonte;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Application.Fontes.ListarFontes;
using InvoiSys.Application.Usuarios.AlterarStatusUsuario;
using InvoiSys.Application.Usuarios.CriarUsuario;
using InvoiSys.Application.Usuarios.ListarUsuarios;
using InvoiSys.Application.Usuarios.RedefinirSenha;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiSys.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        services.AddScoped<ICriarFonteUseCase, CriarFonteUseCase>();
        services.AddScoped<IConsultarFonteUseCase, ConsultarFonteUseCase>();
        services.AddScoped<IListarFontesUseCase, ListarFontesUseCase>();
        services.AddScoped<IAtualizarFonteUseCase, AtualizarFonteUseCase>();
        services.AddScoped<IAlterarStatusFonteUseCase, AlterarStatusFonteUseCase>();

        services.AddScoped<IExecutarColetaManualUseCase, ExecutarColetaManualUseCase>();
        services.AddScoped<IConsultarColetaUseCase, ConsultarColetaUseCase>();
        services.AddScoped<IListarColetasUseCase, ListarColetasUseCase>();

        services.AddScoped<IConsultarDocumentoUseCase, ConsultarDocumentoUseCase>();
        services.AddScoped<IListarDocumentosUseCase, ListarDocumentosUseCase>();

        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();

        services.AddScoped<ICriarUsuarioUseCase, CriarUsuarioUseCase>();
        services.AddScoped<IListarUsuariosUseCase, ListarUsuariosUseCase>();
        services.AddScoped<IAlterarStatusUsuarioUseCase, AlterarStatusUsuarioUseCase>();
        services.AddScoped<IRedefinirSenhaUseCase, RedefinirSenhaUseCase>();

        return services;
    }
}
