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

// Recebe o arquivo como DotNetStreamReference, sem copiar para um array .NET.
window.invoisys.baixarStream = async (nomeArquivo, contentType, streamRef) => {
    const buffer = await streamRef.arrayBuffer();
    window.invoisys.baixarArquivo(nomeArquivo, contentType, new Uint8Array(buffer));
};

// Cria uma URL temporária para exibir o arquivo no próprio navegador.
// O tipo vem da API (PDF ou imagem segura); nunca do site de origem.
window.invoisys.criarUrlBlob = async (contentType, streamRef) => {
    const buffer = await streamRef.arrayBuffer();
    return URL.createObjectURL(new Blob([buffer], { type: contentType }));
};

window.invoisys.revogarUrlBlob = (url) => {
    if (url) {
        URL.revokeObjectURL(url);
    }
};
