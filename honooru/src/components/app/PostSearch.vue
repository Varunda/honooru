
<template>
    <div id="post-search-parent">
        <div v-if="type == 'input'" class="input-group">
            <input class="form-control pr-0" placeholder="search..." v-model="search" :id="'search-input-' + ID" @keyup.enter="enterPress" />

            <span class="input-group-append">
                <button class="btn btn-primary" type="button" @click="performSearch">
                    &#128270;
                </button>
            </span>
        </div>

        <textarea v-else-if="type == 'textarea'" v-model="search" :id="'search-input-' + ID" class="form-control px-1">
        </textarea>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";

    import { Loadable, Loading } from "Loading";

    import { ExtendedTag, TagApi, TagSearchResults, ExtendedTagSearchResult } from "api/TagApi";

    //import Tribute, { TributeCollection, TributeItem } from "node_modules/tributejs";
    import Tribute, { TributeCollection, TributeItem } from "lib/tribute";

    export const PostSearch = Vue.extend({
        props: {
            type: { type: String, required: false, default: "input" },
            value: { type: String, required: false }
        },

        data: function() {
            return {
                ID: Math.floor(Math.random() * 100000) as number,

                enterLockout: 0 as number,

                tribute: null as Tribute<ExtendedTagSearchResult> | null,
                search: "" as string,
                searchInput: {} as HTMLElement,
            }
        },

        mounted: function(): void {
            this.search = this.value;

            this.$nextTick(() => {
                this.searchInput = document.getElementById(`search-input-${this.ID}`) as any;
                if (this.searchInput == null || this.searchInput == undefined) {
                    throw `failed to find #search-input-${this.ID}!`;
                }

                this.tribute = new Tribute<ExtendedTagSearchResult>({
                    trigger: "", // don't trigger on anything special
                    menuShowMinLength: 2, // tag search requires at least 2 characters
                    autocompleteMode: true, // autocomplete this, not sure what false does tho
                    allowSpaces: false, // tags can't have spaces

                    // attribute from a |ExtendedTag| that is inserted into the <textarea>
                    fillAttr: "name",

                    // big government doesn't want you to know this,
                    // but despite it being named |itemClass|, you can in fact put classes in here
                    itemClass: "bg-dark border",
                    // now this one does require you to not have spaces
                    selectClass: "fw-bold",

                    //menuContainer: document.getElementById("post-search-parent") || undefined,
                    //positionMenu: false,

                    // required, otherwise remote search doesn't work
                    searchOpts: {
                        pre: "",
                        post: "",
                        skip: true // this means don't do a local search
                    },

                    noMatchTemplate: () => {
                        return "<li><span class=\"bg-secondary\">not found</span></li>";
                    },

                    // remote callback
                    values: (text: string, callback: (r: ExtendedTagSearchResult[]) => void) => {
                        console.log(`performing search [text=${text}]`);
                        TagApi.search(text).then((value: Loading<TagSearchResults>) => {
                            if (value.state == "loaded") {
                                if (value.data.input != text) {
                                    console.warn(`not updating tag search, ouput does not match input [text=${text}] [input=${value.data.input}]`);
                                } else {
                                    console.log(`loaded searched tags: [${value.data.tags.map(iter => iter.tag.name).join(" ")}]`);
                                    callback(value.data.tags);
                                }
                            }
                        });
                    },

                    // change the menu template to have the color of the tag type
                    menuItemTemplate: (item: TributeItem<ExtendedTagSearchResult>): string => {
                        let name: string = item.original.tag.name;
                        if (item.original.alias != null) {
                            name = `${item.original.alias.alias} -> ` + name;
                        }

                        return `<span contentediable="false" style="color: #${item.original.tag.hexColor}">${name} <span class="text-muted">(${item.original.tag.uses})</span></span>`;
                    }
                });

                this.tribute.attach(this.searchInput);

                // i don't think these actually matter
                this.searchInput.addEventListener("tribute-replaced", (ev: any) => {
                    this.enterLockout = Date.now() + 200;
                    console.log(`replaced event ${JSON.stringify(ev)}, lockout set to ${this.enterLockout}`);
                });

                this.searchInput.addEventListener("tribute-no-match", (ev: any) => {
                    console.log(`no match event emitted`);
                });

                if (this.type == "textarea") {
                    this.searchInput.style.height = `${this.searchInput.scrollHeight}px`;
                    this.searchInput.style.overflowY = "hidden";
                    this.searchInput.addEventListener("input", (ev: Event) => {
                        const elem: HTMLElement = ev.target as HTMLElement;
                        elem.style.height = "auto";
                        elem.style.height = elem.scrollHeight + "px";
                    });
                }
            });
        },

        methods: {
            enterPress: function(): void {
                const lockoutDiff: number = this.enterLockout - Date.now();
                console.log(`PostSearch> lockoutDiff is ${lockoutDiff}`);
                if (lockoutDiff >= 0) {
                    console.log(`enter lockout hit (diff is ${lockoutDiff})`);
                    this.enterLockout = Date.now(); // if the lockout is hit once, let the next enter key do the search
                    return;
                }

                const container: HTMLElement | null = document.querySelector(".tribute-container");
                if (container == null) {
                    console.warn(`query .tribute-container nothing returned`);
                    this.performSearch();
                    return;
                }

                if (container.style.display != "none") {
                    console.log(`not sending @do-search, auto-complete tab is opened`);
                    return;
                }

                this.performSearch();
                return;
            },

            performSearch: function(): void {
                console.log(`emiting do search: '${this.search.trim()}'`);
                this.$emit("do-search", this.search.trim());
            },

            focus: function(): void {
                this.$nextTick(() => {
                    this.searchInput.focus();
                });
            },
        },

        watch: {
            search: function(): void {
                this.$emit("input", this.search);
            }
        }
    });
    export default PostSearch;
</script>