using System.Xml.Linq;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Collectors.RssAtom;

internal static class FeedDocumentParser
{
    private const string AtomNamespace =
        "http://www.w3.org/2005/Atom";

    private const string RdfNamespace =
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    private const string Rss10Namespace =
        "http://purl.org/rss/1.0/";

    public static XDocument Parse(string content) =>
        XDocument.Parse(content, LoadOptions.None);

    public static bool EhAtom(XDocument document) =>
        string.Equals(
            document.Root?.Name.LocalName,
            "feed",
            StringComparison.OrdinalIgnoreCase);

    public static TipoFonte? IdentificarTipoValido(XDocument document)
    {
        if (EhAtomValido(document))
            return TipoFonte.Atom;

        if (EhRss2(document) || EhRss10(document))
            return TipoFonte.Rss;

        return null;
    }

    private static bool EhAtomValido(XDocument document) =>
        EhAtom(document) &&
        string.Equals(
            document.Root?.Name.NamespaceName,
            AtomNamespace,
            StringComparison.Ordinal);

    private static bool EhRss2(XDocument document)
    {
        var root = document.Root;

        return root is not null &&
            string.Equals(
                root.Name.LocalName,
                "rss",
                StringComparison.OrdinalIgnoreCase) &&
            root.Elements().Any(element =>
                string.Equals(
                    element.Name.LocalName,
                    "channel",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool EhRss10(XDocument document) =>
        document.Root is not null &&
        string.Equals(
            document.Root.Name.LocalName,
            "RDF",
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            document.Root.Name.NamespaceName,
            RdfNamespace,
            StringComparison.Ordinal) &&
        document.Root.Elements().Any(element =>
            string.Equals(
                element.Name.LocalName,
                "channel",
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                element.Name.NamespaceName,
                Rss10Namespace,
                StringComparison.Ordinal));
}
