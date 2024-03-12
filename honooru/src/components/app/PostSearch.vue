
<template>
    <div>
        <label>
            Search
        </label>

        <div class="input-group">
            <input class="form-control pr-0" placeholder="search..." v-model="search" id="search-input"
                @keyup.up="keyUp" @keyup.down="keyDown" @keyup.enter="selectEnter" @keydown.tab.prevent="selectFirst"
            />

            <span class="input-group-append">
                <button class="btn btn-primary" type="button">
                    &#128270;
                </button>
            </span>
        </div>

        <div>
            <div v-if="searchResults.state == 'loaded'" class="list-group list-group-sm">
                <div v-for="(result, index) in searchResults.data.tags" class="list-group-item d-flex justify-content-between align-items-center"
                     :class="{ 'list-group-item-primary': select.index == index }"
                     :style="{ 'background-color': '#' + result.hexColor }">

                    {{result.name}}
                    <span class="text-muted">
                        ({{result.uses}})
                    </span>
                </div>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";

    import { Loadable, Loading } from "Loading";

    import { ExtendedTag, TagApi, TagSearchResults } from "api/TagApi";

    export const PostSearch = Vue.extend({
        props: {

        },

        data: function() {
            return {
                search: "" as string,
                searchInput: {} as HTMLElement,

                searchTerm: "" as string,

                searchResults: Loadable.idle() as Loading<TagSearchResults>,

                select: {
                    index: -1 as number
                }
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.searchInput = document.getElementById("search-input") as any;
                if (this.searchInput == null || this.searchInput == undefined) {
                    throw `failed to find #search-input!`;
                }
            });
        },

        methods: {
            performSearch: function(): void {
                this.$emit("do-search", this.search.trim());
            },

            keyUp: function(): void {
                if (this.select.index > -1) {
                    --this.select.index;
                }
            },

            keyDown: function(): void {
                if (this.searchResults.state != "loaded") {
                    return;
                }

                if (this.select.index < this.searchResults.data.tags.length) {
                    ++this.select.index;
                }
            },

            selectFirst: function(): void {
                if (this.select.index == -1) {
                    this.select.index = 0;
                }

                this.selectEnter();
            },

            selectEnter: function(): void {
                if (this.select.index == -1) {
                    console.log(`performing search`);
                    this.performSearch();
                    return;
                }

                if (this.searchResults.state != "loaded") {
                    console.warn(`search results not loaded, cannot add tag to search`);
                    return;
                }

                if (this.select.index > this.searchResults.data.tags.length) {
                    console.warn(`index is out of range [index=${this.select.index}] [length=${this.searchResults.data.tags.length}`);
                    return;
                }

                const tag: ExtendedTag | undefined = this.searchResults.data.tags[this.select.index];
                if (tag == undefined) {
                    console.warn(`not sure why tag is undefined now`);
                    return;
                }

                console.log(`selected tag: ${tag.name}`);

                const [index, indexEnd] = this.getCurrentWordIndex();
                console.log(`replacing ${index} to ${indexEnd} with ${tag.name}`);

                this.search = this.search.slice(0, index) + " " + tag.name + " " + this.search.slice(indexEnd);
                this.searchResults = Loadable.idle();
                this.select.index = -1;
            },

            getCurrentWordIndex: function(): [number, number] {
                const indexEnd = (this.searchInput as any).selectionEnd as number;

                const s: string = this.search;
                for (let i = indexEnd - 1; i >= 0; --i) {
                    const ic: string | undefined = s.at(i);
                    if (ic == " " || ic == undefined) {
                        return [i, indexEnd];
                    }
                }

                return [0, indexEnd];
            },

            getCurrentWord: function(): string {
                const [index, indexEnd] = this.getCurrentWordIndex();
                return this.search.slice(index, indexEnd);
            }
        },

        watch: {
            search: function(): void {
                const cursorStart = (this.searchInput as any).selectionStart as number;
                const cursorEnd = (this.searchInput as any).selectionEnd as number;

                if (cursorStart != cursorEnd) {
                    console.log(`not performing search, there is a selection`);
                    return;
                }

                const s: string = this.search;

                let doSearch: boolean = cursorEnd == s.length || s.at(cursorEnd + 1) == " ";
                if (doSearch == false) {
                    return;
                }

                let word = this.getCurrentWord().trim();

                console.log(`search term: ${word}`);

                if (word.length <= 1) {
                    return;
                }

                this.searchTerm = word;
                this.select.index = -1;
                TagApi.search(word).then((value: Loading<TagSearchResults>) => {
                    if (value.state == "loaded") {
                        if (value.data.input == this.searchTerm) {
                            this.searchResults = value;
                            console.log(`loaded searched tags: [${value.data.tags.map(iter => iter.name).join(" ")}]`);
                        } else {
                            console.log(`ignoring old tag search ${value.data.input}`);
                        }
                    }
                });
            }
        }
    });
    export default PostSearch;
</script>