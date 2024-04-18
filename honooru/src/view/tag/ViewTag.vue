<template>
    <div>
        <app-menu></app-menu>

        <hr class="border" />

        <div v-if="tag.state == 'idle'"></div>

        <div v-else-if="tag.state == 'loading'">
            loading...
        </div>

        <div v-else-if="tag.state == 'loaded'">

            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 0.5rem;">
                <div class="border-right mr-3">
                    controls

                    <div class="mr-3">
                        <button class="btn btn-primary d-block w-100" @click="recountPostUsage">
                            re-count post usage
                        </button>
                    </div>
                </div>

                <div>
                    <div>
                        <span :style="{ 'color': '#' + tag.data.hexColor }" class="h2 d-inline">
                            {{tag.data.name}}
                        </span>

                        <span>
                            used in <b>{{tag.data.uses | locale(0)}}</b> posts
                        </span>
                    </div>

                    <div class="mb-3">
                        <button v-if="editing == false" class="btn btn-primary" @click="startEdit">
                            edit
                        </button>

                        <button v-if="editing == true" class="btn btn-success" @click="saveEdit">
                            save
                        </button>

                        <button v-if="editing == true" class="btn btn-secondary" @click="cancelEdit">
                            cancel
                        </button>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0 h5"><strong>name</strong></label>
                        <input v-if="editing" class="form-control" v-model="tagCopy.name" />
                        <input v-else disabled readonly class="form-control-plaintext" v-model="tagCopy.name" />
                    </div>

                    <div class="mb-2">
                        <label class="mb-0 h5"><strong>type</strong></label>
                        <input v-if="!editing"  disabled readonly class="form-control-plaintext" v-model="tagCopy.typeName" />

                        <template v-else>
                            <div v-if="tagTypes.state == 'loading'">
                                loading...
                            </div>

                            <div v-else-if="tagTypes.state == 'loaded'">
                                <select class="form-control" v-model="tagCopy.typeID">
                                    <option v-for="type in tagTypes.data" :value="type.id">
                                        {{type.name}}
                                    </option>
                                </select>
                            </div>
                        </template>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">description</label>
                        <textarea :disabled="!editing" :readonly="!editing" class="form-control" v-model="tagCopy.description"></textarea>
                    </div>

                    <div class="mb-2">
                        <label class="d-block">
                            quick change type
                            <info-hover text="click to quickly change the type of this tag"></info-hover>
                        </label>

                        <div v-if="tagTypes.state == 'loaded'" class="btn-group">
                            <button v-for="type in tagTypes.data" class="btn btn-sm" :style="{ 'background-color': '#' + type.hexColor }" @click="changeType(type.id)">
                                {{type.name}}
                            </button>
                        </div>
                    </div>

                    <div v-if="editing">
                        <button class="btn btn-success" @click="saveEdit">
                            save
                        </button>
                    </div>

                    <hr class="border" />

                    <div>
                        <div class="mb-3">
                            <h4 class="mb-0">
                                aliases
                                <span v-if="aliases.state == 'loaded'" class="text-muted">
                                    ({{aliases.data.length}})
                                </span>
                            </h4>
                            <h6 class="text-muted">
                                aliases are words that are equivalent to this tag. when creating tags, any tag with these words will instead use this tag
                            </h6>
                        </div>

                        <div class="mb-3">
                            <div v-if="aliases.state == 'idle'"></div>
                            <div v-else-if="aliases.state == 'loading'">
                                loading...
                            </div>

                            <div v-else-if="aliases.state == 'loaded'">
                                <div v-for="a in aliases.data" class="d-inline mr-1">
                                    <div class="btn-group">
                                        <button v-if="del.alias == a.alias" class="btn btn-danger" @click="deleteAlias(a.alias)">
                                            <span class="bi-trash"></span>
                                        </button>

                                        <button class="btn btn-info">
                                            {{a.alias}}
                                        </button>

                                        <button v-if="del.alias != a.alias" class="btn btn-info" @click="del.alias = a.alias">
                                            &times;
                                        </button>

                                        <button v-else class="btn btn-secondary" @click="del.alias = ''">
                                            &times;
                                        </button>
                                    </div>
                                </div>
                            </div>

                            <div v-else-if="aliases.state == 'error'">
                                <api-error :error="aliases.problem"></api-error>
                            </div>
                        </div>

                        <div>
                            <label class="mb-0">create new</label>
                            <div class="input-group">
                                <input class="form-control" v-model="input.alias" @keyup.enter="createAlias" />
                                <span class="input-group-append">
                                    <button class="btn btn-primary" @click="createAlias">
                                        insert
                                    </button>
                                </span>
                            </div>

                            <api-error v-if="pending.alias.state == 'error'" :error="pending.alias.problem"></api-error>
                        </div>
                    </div>

                    <hr class="border" />

                    <div>
                        <div class="mb-3">
                            <h4 class="mb-0">
                                implications
                            </h4>
                            <h6 class="text-muted">
                                implications are tags that when added to a post, will add another tag as well.
                                implications only go one level deep.
                                <br />
                                for example, if there is an implication A -> B -> C, if tag A is added to a post,
                                only tag B will be added as well, NOT tag C, despite an implication from tag B to tag C
                            </h6>
                        </div>

                        <div class="mb-3">
                            <view-tag-implications :tag-id="tagID"></view-tag-implications>
                        </div>

                    </div>

                    <hr class="border" />

                    <h2>
                        <a :href="'/posts?q=' + tag.data.name">
                            recent posts
                        </a>
                    </h2>

                    <post-list :q="tag.data.name + ' sort:id_desc'" :limit="10"></post-list>
                </div>
            </div>

        </div>

        <div v-else-if="tag.state == 'error'">
            failed to load tag
            <api-error :error="tag.problem"></api-error>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import ToggleButton from "components/ToggleButton";
    import PostList from "components/app/PostList.vue";

    import ViewTagImplications from "./components/ViewTagImplications.vue";

    import "filters/LocaleFilter";

    import { Post, PostApi } from "api/PostApi";
    import { Tag, ExtendedTag, TagApi } from "api/TagApi";
    import { PostTagApi } from "api/PostTagApi";
    import { TagTypeApi, TagType } from "api/TagTypeApi";
    import { TagAlias, TagAliasApi } from "api/TagAliasApi";
    import { TagImplicationBlock, TagImplicationApi } from "api/TagImplicationApi";

    export const ViewTag = Vue.extend({
        props: {

        },

        data: function() {
            return {
                tagID: 0 as number,
                tag: Loadable.idle() as Loading<ExtendedTag>,
                tagCopy: new ExtendedTag() as ExtendedTag,

                editing: false as boolean,

                tagTypes: Loadable.idle() as Loading<TagType[]>,

                aliases: Loadable.idle() as Loading<TagAlias[]>,

                input: {
                    alias: "" as string
                },

                pending: {
                    alias: Loadable.idle() as Loading<void>,
                },

                del: {
                    alias: "" as string,
                }
            }
        },

        created: function(): void {
            this.getTagID();
            this.getTag();

            this.getTagTypes();
        },

        mounted: function(): void {
            document.title = "Honooru / Tag";

            document.addEventListener("keyup", (ev: KeyboardEvent) => {
                // this means another input is currently in focus
                if (document.activeElement != document.body) {
                    return;
                }

                if (ev.key == "e") {
                    this.startEdit();
                }

                if (ev.key == "Enter" && ev.ctrlKey) {
                    this.saveEdit();
                }
            });
        },

        methods: {
            getTagID: function(): void {
                const parts: string[] = location.pathname.split("/");
                console.log(parts);
                if (parts.length < 3) {
                    console.error(`missing tag ID`);
                    return;
                }

                this.tagID = Number.parseInt(parts[2]);

                if (Number.isNaN(this.tagID)) {
                    throw `failed to parse ${parts[2]} to a valid int`;
                }
            },

            getTag: async function(): Promise<void> {
                this.tag = Loadable.loading();
                this.tag = await TagApi.getExtendedByID(this.tagID);

                this.getTagAliases(this.tagID);

                if (this.tag.state == "loaded") {
                    document.title = `Honooru / Tag: ${this.tag.data.name}`;
                    this.tagCopy = { ...this.tag.data };
                }
            },

            getTagTypes: async function(): Promise<void> {
                this.tagTypes = Loadable.loading();
                this.tagTypes = await TagTypeApi.getAll();

                if (this.tagTypes.state == "loaded") {
                    this.tagTypes = Loadable.loaded(this.tagTypes.data.sort((a, b) => {
                        return a.name.localeCompare(b.name);
                    }));
                }
            },

            getTagAliases: async function(tagID: number): Promise<void> {
                this.aliases = Loadable.loading();
                this.aliases = await TagAliasApi.getByTagID(tagID);
            },

            startEdit: function(): void {
                this.editing = true;
            },

            saveEdit: async function(): Promise<void> {
                this.editing = false;

                const r: Loading<Tag> = await TagApi.update(this.tagID, this.tagCopy);

                if (r.state == "loaded") {
                    Toaster.add("updated tag", "successfully updated tag", "success");
                    await this.getTag();
                } else if (r.state == "error") {
                    Toaster.add("error!", `failed to update tag: ${r.problem.detail}`, "danger");
                }
            },

            cancelEdit: function(): void {
                this.editing = false;

                if (this.tag.state == "loaded") {
                    this.tagCopy = { ...this.tag.data };
                }
            },

            createAlias: async function(): Promise<void> {
                this.pending.alias = Loadable.loading();
                this.pending.alias = await TagAliasApi.insert(this.input.alias, this.tagID);

                if (this.pending.alias.state == "loaded") {
                    this.getTagAliases(this.tagID);
                }
            },

            deleteAlias: async function(): Promise<void> {
                const r: Loading<void> = await TagAliasApi.delete(this.del.alias);
                if (r.state == "loaded") {
                    Toaster.add(`alias deleted`, `successfully deleted the alias '${this.del.alias}'`, "success");
                    this.del.alias = "";

                    this.getTagAliases(this.tagID);
                }
            },

            recountPostUsage: async function(): Promise<void> {
                Toaster.add(`update queued`, "queued an updated to the tag usage count", "info");
                await TagApi.queueRecount(this.tagID);
            },

            changeType: async function(typeID: number): Promise<void> {
                this.tagCopy.typeID = typeID;
                await this.saveEdit();
            }
        },

        computed: {

        },

        components: {
            InfoHover, ApiError,
            AppMenu,
            ToggleButton, PostList,
            ViewTagImplications
        }
    });
    export default ViewTag;

</script>