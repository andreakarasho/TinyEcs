use proc_macro::TokenStream;
use proc_macro2::Span;
use quote::quote;
use syn::parse::{Parse, ParseStream};
use syn::{Ident, Result, Token};

#[derive(Debug)]
pub struct SimpleInteractionPluginParams {
    pub controlled_component: syn::Ident,
    pub target_type: syn::Ident,
    pub target_prop: Option<syn::LitStr>,
}

impl Parse for SimpleInteractionPluginParams {
    fn parse(input: ParseStream) -> Result<Self> {
        let content;
        syn::parenthesized!(content in input);

        let controlled_component = content.parse()?;
        content.parse::<Token![,]>()?;
        let target_type = content.parse()?;

        let target_prop;
        if let Ok(_) = content.parse::<Token![,]>() {
            target_prop = Some(content.parse()?);
        } else {
            target_prop = None;
        }

        Ok(SimpleInteractionPluginParams {
            controlled_component,
            target_type,
            target_prop,
        })
    }
}

pub(crate) fn impl_simple_interaction_plugin_macro(attr: TokenStream, name: Ident) -> TokenStream {
    let params = syn::parse_macro_input!(attr as SimpleInteractionPluginParams);

    let component = params.controlled_component;
    let target_type = params.target_type;

    let controller_belly;

    if let Some(target_prop) = params.target_prop {
        let target_prop_id = Ident::new(target_prop.value().as_str(), Span::call_site());

        controller_belly = quote! {
            fn extract_value(from: &Self::ControlledComponent) -> Self::TargetType {
                from.#target_prop_id
            }

            fn update_controlled_component(
                mut controlled_component: Mut<'_, Self::ControlledComponent>,
                new_value: Self::TargetType,
            ) {
                controlled_component.#target_prop_id = new_value;
            }
        };
    } else {
        controller_belly = quote! {
            fn extract_value(from: &Self::ControlledComponent) -> Self::TargetType {
                from.0
            }

            fn update_controlled_component(
                mut controlled_component: Mut<'_, Self::ControlledComponent>,
                new_value: Self::TargetType,
            ) {
                controlled_component.0 = new_value;
            }
        };
    }

    let state_name = name.to_string().clone() + "State";
    let state_ident = Ident::new(state_name.as_str(), Span::call_site());

    let gen = quote! {
        #[derive(Component, Debug, Default, Reflect)]
        pub struct #name {
            pub highlight: Option<#target_type>,
            pub pressed: Option<#target_type>,
            pub cancel: Option<#target_type>,
        }

        impl InteractionConfig for #name {
            type TargetType = #target_type;

            fn new(
                highlight: Option<Self::TargetType>,
                pressed: Option<Self::TargetType>,
                cancel: Option<Self::TargetType>,
            )-> Self{
                Self{
                    highlight, pressed, cancel
                }
            }

            fn highlight(&self) -> Option<Self::TargetType> {
                self.highlight
            }

            fn pressed(&self) -> Option<Self::TargetType> {
                self.pressed
            }

            fn cancel(&self) -> Option<Self::TargetType> {
                self.cancel
            }
        }

        #[derive(Component)]
        pub struct #state_ident {
            original: #target_type,
            transition_base: #target_type,
        }

        impl InteractionState for #state_ident {
            type TargetType = #target_type;

            fn original(&self) -> Self::TargetType {
                self.original
            }
            fn transition_base(&self) -> Self::TargetType {
                self.transition_base
            }
            fn set_original(&mut self, from: Self::TargetType) {
                self.original = from;
            }
            fn set_transition_base(&mut self, from: Self::TargetType) {
                self.transition_base = from;
            }
        }

        impl ComponentController for #name {
            type TargetType = #target_type;
            type InteractionState = #state_ident;
            type ControlledComponent =  #component;

            fn state(from: &Self::ControlledComponent) -> Self::InteractionState {
                Self::InteractionState {
                    original: Self::extract_value(from),
                    transition_base: Self::extract_value(from),
                }
            }

            #controller_belly
        }

        impl Plugin for #name {
            fn build(&self, app: &mut App) {
                app.add_systems(
                    PreUpdate,
                    (
                        add_animated_interaction_state::<#name>,
                        add_interactive_state::<
                            #name,
                            #state_ident,
                            #component,
                        >,
                    ),
                )
                .add_systems(
                    Update,
                    update_animated_interaction_state::<#name>
                        .in_set(AnimatedInteractionUpdate),
                )
                .add_systems(
                    Update,
                    (
                        update_transition_base_state::<
                            #name,
                            #state_ident,
                            #component,
                            #target_type,
                        >,
                        update_controlled_component::<
                            #name,
                            #state_ident,
                            #component,
                            #target_type,
                        >,
                    )
                        .chain()
                        .after(AnimatedInteractionUpdate),
                );
            }
        }
    };
    gen.into()
}
