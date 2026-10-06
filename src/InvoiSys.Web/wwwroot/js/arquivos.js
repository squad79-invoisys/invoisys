window.invoisys = window.invoisys || {};

window.invoisys.baixarArquivo = (nomeArquivo, contentType, conteudo) => {
    const blob = new Blob([conteudo], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = nomeArquivo;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
};
