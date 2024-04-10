<template>
    <div>
        <app-menu></app-menu>

        <div>
            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 1rem;">
                <div>
                    <post-search v-model="query" @keyup.enter="performSearch" @do-search="performSearch"></post-search>

                    <div v-if="posts.state == 'loaded'" class="no-underline-links font-monospace">
                        <div v-for="tag in postTags" :style="{ 'color': '#' + tag.hexColor }">
                            <a :href="'/tag/' + tag.id" :class="{ 'text-success': tag.description, 'text-secondary': !tag.description }">
                                <info-hover :text="tag.description || ''"></info-hover>
                            </a>

                            <span class="text-light">
                                <!-- because this font is monospace, all spaces make this look weird, and white-space-collapse is not in firefox yet -->
                                <a href="javascript:void;" @click="addTag(tag.name)">+</a>
                                <a href="javascript:void;" @click="removeTag(tag.name)">-</a>
                            </span>

                            <a :href="'/posts?q=' + tag.name">
                                <span :style="{ 'color': '#' + tag.hexColor }">
                                    {{tag.name}}
                                </span>
                            </a>
                            <span class="text-muted">
                                ({{tag.uses}})
                            </span>
                        </div>
                    </div>
                </div>

                <div>
                    <post-list :q="search" @search-done="searchDone"></post-list>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    import { SearchResults } from "api/PostApi";
    import { ExtendedTag } from "api/TagApi";

    export const Posts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                query: "" as string,
                search: "" as string,

                usedTags: new Set as Set<string>,

                posts: Loadable.idle() as Loading<SearchResults>
            }
        },

        beforeMount: function(): void {
            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "";
            }

            this.usedTags = new Set(this.query.trim().toLowerCase().split(" "));

            this.search = this.query;
        },

        methods: {
            performSearch: function(query: string): void {
                console.log(`performing search [query=${query}]`);
                const params: URLSearchParams = new URLSearchParams();
                params.set("q", query);
                location.href = `/posts?${params.toString()}`;
            },

            searchDone: function(data: Loading<SearchResults>): void {
                console.log("done!");
                this.posts = data;
            },

            addTag: function(tag: string): void {
                this.performSearch(this.query + " " + tag);
            },

            removeTag: function(tag: string): void {
                this.query = this.query.replace(tag, " ");
                this.query += " -" + tag;
                this.performSearch(this.query);
            }
        },

        computed: {
            postTags: function(): ExtendedTag[] {
                if (this.posts.state != "loaded") {
                    return [];
                }

                return [...this.posts.data.tags].sort((a: ExtendedTag, b: ExtendedTag) => {
                    // ensure that the tags used in the query are included in the tag list first
                    const au: number = this.usedTags.has(a.name) == false ? 0 : 1;
                    const bu: number = this.usedTags.has(b.name) == false ? 0 : 1;

                    return bu - au
                        || b.uses - a.uses
                        || a.name.localeCompare(b.name);
                }).slice(0, 30);
            },

            linkTags: function(): string {
                return encodeURI(Array.from(this.usedTags).join(" "));
            }
        },

        components: {
            InfoHover, ApiError,
            AppMenu,
            PostList,
            PostSearch
        }
    });
    export default Posts;

</script>