
import { Loadable, Loading } from "Loading";
import { ExtendedTag, TagApi } from "api/TagApi";
import { marked, MarkedExtension, Token } from "marked";
import * as DOMPurify from "dompurify";

const DEBUG: boolean = false;

const _debug = (str: string): void => {
    if (DEBUG) {
        console.log(str);
    }
}

const rule = /^post #(\d+)/;
const tagRule = /^\[\[(.+?)\]\]/; // use a non-greedy regex here in case multiple [[tags]] are used

const honooruMarkdownExtension: MarkedExtension = {
    // walks the tokens and turns a honooru-tag-link into an <a> with the correct ID
    walkTokens: async (token) => {
        _debug(`${token.type} :: ${token.raw}`);

        if (token.type != "honooru-tag-link") {
            return;
        }

        const tagName: string = token.tagName;
        _debug(`getting tag [tagName='${tagName}']`);

        const tag: Loading<ExtendedTag> = await TagApi.getByName(tagName);

        _debug(`loaded tag [state=${tag.state}]`);
        if (tag.state == "loaded") {
            token.tagData = tag.data;
        } else if (tag.state == "nocontent") {
            token.tagData = {
                id: -1,
                name: `NOT_FOUND:${tagName}`,
                typeID: -1,
                typeName: "invalid",
                hexColor: "ff0000",
                uses: -1,
                description: ""
            };
        }
    },

    // async lets walkTokens above be async, which lets us get the tag data when we find a honooru-tag-link
    async: true,

    extensions: [
        // extension to turn post numbers into links
        // example:
        //          post #4
        //      would turn into
        //          <a href="/post/4">post 4</a>
        //
        {
            name: "honooru-post-link",
            start: (src: string) => src.indexOf("post #"),
            level: "inline",
            tokenizer: function(src: string, tokens: Token[]) {
                _debug(`SRC "${src}"`);

                const match = rule.exec(src);
                if (match) {
                    const token = {
                        type: "link",
                        raw: match[0],
                        text: match[0].trim(),
                        href: `/post/${match[1]}`,
                        tokens: [{
                            type: "text",
                            raw: match[0],
                            text: match[0]
                        }]
                    }

                    return token;
                }

                return undefined;
            },
        },

        // extension to turn tags into links
        // example:
        //          [[tag]]
        //      would turn into
        //          <a href="/post/1">tag</a>
        // with the correct coloring to match the tag type, and the correct tag ID that matches the name
        {
            name: "honooru-tag-link",
            start: (src: string) => src.indexOf("[["),
            level: "inline",
            tokenizer: function(src: string, tokens: Token[]) {
                const match = tagRule.exec(src);

                if (!match) {
                    return undefined;
                }

                const token = {
                    type: "honooru-tag-link",
                    raw: match[0],
                    text: match[1].trim(),
                    tagName: match[1].trim(), // used in walkTokens
                    tokens: [{
                        type: "text",
                        raw: match[0],
                        text: match[1]
                    }]
                };

                return token;
            },

            // called after walkTokens has populated the data we want
            renderer: (token) => {
                _debug(`renderer for tag link: ${token.tagData}`);
                return `<a href="/tag/${token.tagData.id}" style="color: #${token.tagData.hexColor}">${token.tagData.name}</a>`;
            }
        }
    ],

    tokenizer: null,
};

export default class MarkdownUtil {

    /**
     * turn an input string into a sanitized HTML output that can be rendered as markdown.
     * include some custom Honooru output as well:
     *      post #{NUMBER} => will create a link to that post
     *      [[{TAG}]] => will create a link to that tag 
     * 
     * @param input input markdown text
     * @returns output HTML that has been sanitized
     */
    public static async markdown(input: string): Promise<string> {
        marked.use(honooruMarkdownExtension);

        const html: string | Promise<string> = marked.parse(input);

        try {
            if (typeof html == "string") {
                return DOMPurify.sanitize(html);
            } else {
                return DOMPurify.sanitize(await html);
            }
        } catch (err) {
            console.error(`error when creating markdown`);
            throw err;
        }
    }

}